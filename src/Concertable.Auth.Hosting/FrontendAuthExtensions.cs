using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Concertable.Auth.Hosting;

public static class FrontendAuthExtensions
{
    extension<T>(IResourceBuilder<T> auth)
        where T : IResourceWithEnvironment
    {
        public IResourceBuilder<T> WithSpaClients(IReadOnlyList<(global::SpaSurface Surface, string ClientName)> registrations)
        {
            var clients = registrations.Select(registration =>
            {
                if (string.IsNullOrWhiteSpace(registration.ClientName))
                    throw new ArgumentException(
                        $"SPA surface '{registration.Surface.ResourceName}' does not define an auth client.",
                        nameof(registrations));

                return registration;
            }).ToArray();

            return auth.WithEnvironment(context =>
            {
                foreach (var key in context.EnvironmentVariables.Keys
                    .Where(key => key.StartsWith("Auth__SpaClients__", StringComparison.Ordinal)).ToArray())
                    context.EnvironmentVariables.Remove(key);

                context.EnvironmentVariables["Auth__SpaClients__RestrictToEnabledClients"] = "true";
                for (var index = 0; index < clients.Length; index++)
                {
                    var (surface, client) = clients[index];
                    context.EnvironmentVariables[$"Auth__SpaClients__EnabledClients__{index}"] = client;
                    context.EnvironmentVariables[$"Auth__SpaClients__{client}__RedirectUri"] = $"{surface.Origin}/auth/callback";
                    context.EnvironmentVariables[$"Auth__SpaClients__{client}__PostLogoutRedirectUri"] = surface.Origin;
                    context.EnvironmentVariables[$"Auth__SpaClients__{client}__AllowedCorsOrigins__0"] = surface.Origin;
                }
            });
        }

        public IResourceBuilder<T> WithMobilePublicUrl(EndpointReference publicEndpoint) =>
            auth.WithEnvironment("Auth__PublicUrl", publicEndpoint);

        public IResourceBuilder<T> WithSpaClient((global::SpaSurface Surface, string ClientName) registration)
        {
            var (surface, client) = registration;
            if (string.IsNullOrWhiteSpace(client))
                throw new ArgumentException(
                    $"SPA surface '{surface.ResourceName}' does not define an auth client.",
                    nameof(registration));

            return auth.WithEnvironment($"Auth__SpaClients__{client}__RedirectUri", $"{surface.Origin}/auth/callback")
                       .WithEnvironment($"Auth__SpaClients__{client}__PostLogoutRedirectUri", surface.Origin)
                       .WithEnvironment($"Auth__SpaClients__{client}__AllowedCorsOrigins__0", surface.Origin);
        }
    }
}
