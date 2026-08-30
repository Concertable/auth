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
