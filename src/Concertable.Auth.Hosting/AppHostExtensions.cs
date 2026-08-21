using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Microsoft.Extensions.Configuration;

namespace Concertable.Auth.Hosting;

public static class AppHostExtensions
{
    public static IResourceBuilder<ProjectResource> AddAuth<TProject>(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<SqlServerDatabaseResource> authDb,
        IResourceBuilder<SqlServerDatabaseResource> b2bDb,
        IResourceBuilder<AzureServiceBusResource> asb)
        where TProject : IProjectMetadata, new()
    {
        var auth = builder.AddProject<TProject>(AuthConstants.Resource)
                          .WithReference(authDb)
                          .WaitFor(authDb)
                          .WithReference(b2bDb)
                          .WithReference(asb)
                          .WaitFor(asb)
                          .AddSecrets(builder, "ServiceAuth:B2BClientSecret", "ServiceAuth:CustomerClientSecret", "ServiceAuth:AuthClientSecret");

        auth.WithEnvironment("Auth__Authority", auth.GetEndpoint("https"));
        WithLocalSpaClient(auth, "Customer", 5174);
        WithLocalSpaClient(auth, "Venue", 5175);
        WithLocalSpaClient(auth, "Artist", 5176);
        WithLocalSpaClient(auth, "Admin", 5178);

        var lanIp = builder.Configuration["MobileLanIp"];
        if (!string.IsNullOrEmpty(lanIp))
        {
            auth.WithEnvironment("Auth__ExpoGoRedirectUri__Customer", $"exp://{lanIp}:8082");
            auth.WithEnvironment("Auth__ExpoGoRedirectUri__Business", $"exp://{lanIp}:8083");
        }

        return auth;
    }

    private static void WithLocalSpaClient(
        IResourceBuilder<ProjectResource> auth,
        string client,
        int port)
    {
        var origin = $"https://localhost:{port}";

        auth.WithEnvironment($"Auth__SpaClients__{client}__RedirectUri", $"{origin}/auth/callback")
            .WithEnvironment($"Auth__SpaClients__{client}__PostLogoutRedirectUri", origin)
            .WithEnvironment($"Auth__SpaClients__{client}__AllowedCorsOrigins__0", origin);
    }
}
