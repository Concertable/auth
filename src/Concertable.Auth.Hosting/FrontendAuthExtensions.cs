using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Concertable.Auth.Hosting;

public static class FrontendAuthExtensions
{
    extension<T>(IResourceBuilder<T> auth)
        where T : IResourceWithEnvironment
    {
        public IResourceBuilder<T> WithMobilePublicUrl() =>
            auth.WithEnvironment(context =>
            {
                if (context.EnvironmentVariables.TryGetValue("services__auth__https__0", out var authUrl))
                    context.EnvironmentVariables["Auth__PublicUrl"] = authUrl;
            });

        public IResourceBuilder<T> WithSpaClient(SpaSurface surface)
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
