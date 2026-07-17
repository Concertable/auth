namespace Concertable.Auth.Data;

// Design-time only: the fallback `dotnet ef` uses when Aspire hasn't injected ConnectionStrings__B2BDb.
// Real hosts always resolve the string from configuration; this is the local/dev fallback.
internal static class DesignTimeConnectionString
{
    public static string B2B() =>
        Environment.GetEnvironmentVariable("ConnectionStrings__B2BDb")
        ?? "Server=localhost,1433;Database=concertable-b2b;User Id=sa;Password=Password11!;TrustServerCertificate=True";
}
