using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Microsoft.Extensions.Configuration;

namespace Concertable.Auth.Hosting;

public static class AppHostExtensions
{
    public static IResourceBuilder<ServiceContainerResource> AddAuth(
        this IDistributedApplicationBuilder builder,
        string image,
        string digest,
        IResourceBuilder<SqlServerDatabaseResource> authDb,
        IResourceBuilder<AzureServiceBusResource> asb)
    {
        var auth = builder.AddContainerImage(AuthConstants.Resource, image, digest)
                          .WithReference(authDb)
                          .WaitFor(authDb)
                          .WithReference(asb)
                          .WaitFor(asb)
                          .AddSecrets(builder, "ServiceAuth:B2BClientSecret", "ServiceAuth:CustomerClientSecret", "ServiceAuth:AuthClientSecret");

        // The pinned pre-cutover Auth image serves HTTPS on its container port but ships no certificate.
        // Hand it the ASP.NET Core development certificate at run time (dev + E2E); publish mode is
        // unaffected. This bridge is removed with the `--user root` argument once a corrected Auth image
        // and digest land (see RT3 progress notes).
#pragma warning disable ASPIRECERTIFICATES001 // experimental API; scoped to the temporary Auth image bridge
        auth.WithHttpsDeveloperCertificate();
#pragma warning restore ASPIRECERTIFICATES001

        // The image binds HTTP only (ASPNETCORE_HTTP_PORTS=8080 and no HTTPS port), so the certificate
        // alone left Kestrel serving plaintext on the port every AppHost declares as its `https`
        // endpoint: an https consumer got `Cannot determine the frame size or a corrupted frame was
        // received`. ASPNETCORE_URLS outranks ASPNETCORE_HTTP_PORTS, so this is what actually opens TLS
        // there. Run mode only, so a published manifest never inherits the developer certificate.
        if (builder.ExecutionContext.IsRunMode)
            auth.WithEnvironment("ASPNETCORE_URLS", AuthConstants.ContainerHttpsUrl);

        auth.WithEnvironment("Auth__Authority", auth.GetEndpoint("https"));
        foreach (var client in LocalSpaSurfaces.Authenticated)
            auth.WithLocalSpaClient(client);

        var lanIp = builder.Configuration["MobileLanIp"];
        if (!string.IsNullOrEmpty(lanIp))
        {
            auth.WithEnvironment("Auth__ExpoGoRedirectUri__Customer", $"exp://{lanIp}:8082");
            auth.WithEnvironment("Auth__ExpoGoRedirectUri__Business", $"exp://{lanIp}:8083");
        }

        return auth;
    }

    public static IResourceBuilder<ProjectResource> AddAuth<TProject>(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<SqlServerDatabaseResource> authDb,
        IResourceBuilder<AzureServiceBusResource> asb)
        where TProject : IProjectMetadata, new()
    {
        var auth = builder.AddProject<TProject>(AuthConstants.Resource)
                          .WithReference(authDb)
                          .WaitFor(authDb)
                          .WithReference(asb)
                          .WaitFor(asb)
                          .AddSecrets(builder, "ServiceAuth:B2BClientSecret", "ServiceAuth:CustomerClientSecret", "ServiceAuth:AuthClientSecret");

        auth.WithEnvironment("Auth__Authority", auth.GetEndpoint("https"));
        foreach (var client in LocalSpaSurfaces.Authenticated)
            auth.WithLocalSpaClient(client);

        var lanIp = builder.Configuration["MobileLanIp"];
        if (!string.IsNullOrEmpty(lanIp))
        {
            auth.WithEnvironment("Auth__ExpoGoRedirectUri__Customer", $"exp://{lanIp}:8082");
            auth.WithEnvironment("Auth__ExpoGoRedirectUri__Business", $"exp://{lanIp}:8083");
        }

        return auth;
    }

    extension<T>(IResourceBuilder<T> auth)
        where T : IResourceWithEnvironment
    {
        public IResourceBuilder<T> WithLocalSpaClient(LocalSpaSurface surface)
        {
            var client = surface.AuthClient
                ?? throw new ArgumentException(
                    $"Local SPA surface '{surface.ResourceName}' does not define an auth client.",
                    nameof(surface));

            return auth.WithEnvironment($"Auth__SpaClients__{client}__RedirectUri", $"{surface.Origin}/auth/callback")
                       .WithEnvironment($"Auth__SpaClients__{client}__PostLogoutRedirectUri", surface.Origin)
                       .WithEnvironment($"Auth__SpaClients__{client}__AllowedCorsOrigins__0", surface.Origin);
        }
    }
}
