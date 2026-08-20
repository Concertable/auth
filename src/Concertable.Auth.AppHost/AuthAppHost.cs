using Aspire.Hosting;
using Concertable.Auth.Hosting;
using Concertable.B2B.Hosting;

public static class AuthAppHost
{
    public static IDistributedApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = StrictDistributedApplication.CreateBuilder(args);
        var sql = builder.AddSqlServerContainer("concertable-auth-sql-data");
        var authDb = sql.AddDatabase(AuthConstants.Database);
        var b2bDb = sql.AddDatabase(B2BConstants.Database);
        var asb = builder.AddServiceBus();
        asb.Topology().AddAuthTopology().RunAsEmulator();
        var auth = builder.AddAuth<Projects.Concertable_Auth>(authDb, b2bDb, asb);
        auth.WithEnvironment("ServiceAuth__AuthClientId", "concertable-auth");
        return builder;
    }
}
