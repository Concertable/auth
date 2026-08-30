using Aspire.Hosting;
using Concertable.Auth.Hosting;

public static class AuthAppHost
{
    public static IDistributedApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = StrictDistributedApplication.CreateBuilder(args);
        var sql = builder.AddSqlServerContainer("concertable-auth-sql-data");
        var authDb = sql.AddDatabase(AuthConstants.Database);
        var asb = builder.AddServiceBus();
        asb.Topology().AddAuthTopology().RunAsEmulator();
        var auth = builder.AddAuth<Projects.Concertable_Auth>(authDb, asb);
        auth.WithEnvironment("ServiceAuth__AuthClientId", "concertable-auth");
        return builder;
    }
}
