namespace Concertable.Auth.Data;

// ./initial-migrations.ps1 supplies a parseable AuthDb scaffolding value; live migration jobs resolve their connection separately.
internal static class DesignTimeConfiguration
{
    public static string ConnectionString() =>
        Environment.GetEnvironmentVariable($"ConnectionStrings__{AuthDb.Name}")
        ?? throw new InvalidOperationException(
            $"Design-time connection string 'ConnectionStrings__{AuthDb.Name}' is not set. " +
            "Set it via environment or user-secrets — ./initial-migrations.ps1 exports it for local re-scaffolds.");
}
