using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Microsoft.Extensions.Configuration;

namespace Concertable.Auth.Hosting;

public static class AppHostExtensions
{
    extension(IDistributedApplicationBuilder builder)
    {
        public IResourceBuilder<ServiceContainerResource> AddAuth(
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

            auth.WithEnvironment("Auth__Authority", auth.GetEndpoint("https"));

            var lanIp = builder.Configuration["MobileLanIp"];
            if (!string.IsNullOrEmpty(lanIp))
            {
                auth.WithEnvironment("Auth__ExpoGoRedirectUri__Customer", $"exp://{lanIp}:8082");
                auth.WithEnvironment("Auth__ExpoGoRedirectUri__Business", $"exp://{lanIp}:8083");
            }

            return auth;
        }

        public IResourceBuilder<ProjectResource> AddAuth<TProject>(
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

            var lanIp = builder.Configuration["MobileLanIp"];
            if (!string.IsNullOrEmpty(lanIp))
            {
                auth.WithEnvironment("Auth__ExpoGoRedirectUri__Customer", $"exp://{lanIp}:8082");
                auth.WithEnvironment("Auth__ExpoGoRedirectUri__Business", $"exp://{lanIp}:8083");
            }

            return auth;
        }
    }

}
