using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Microsoft.Extensions.Configuration;

public static class AuthAppHostExtensions
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

        var lanIp = builder.Configuration["MobileLanIp"];
        if (!string.IsNullOrEmpty(lanIp))
        {
            auth.WithEnvironment("Auth__ExpoGoRedirectUri__Customer", $"exp://{lanIp}:8082");
            auth.WithEnvironment("Auth__ExpoGoRedirectUri__Business", $"exp://{lanIp}:8083");
        }

        return auth;
    }
}
