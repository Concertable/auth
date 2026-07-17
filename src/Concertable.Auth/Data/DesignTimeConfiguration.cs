namespace Concertable.Auth.Data;

// Design-time only: the connection string `dotnet ef` uses to build the model (it never opens it).
// Resolved from ConnectionStrings__B2BDb; throws if absent — ./initial-migrations.ps1 exports it locally.
internal static class DesignTimeConfiguration
{
    private const string ConnectionStringName = "B2BDb";

    public static string ConnectionString() =>
        Environment.GetEnvironmentVariable($"ConnectionStrings__{ConnectionStringName}")
        ?? throw new InvalidOperationException(
            $"Design-time connection string 'ConnectionStrings__{ConnectionStringName}' is not set. " +
            "Set it via environment or user-secrets — ./initial-migrations.ps1 exports it for local re-scaffolds.");
}
