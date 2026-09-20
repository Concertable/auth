using Aspire.Hosting;
using Concertable.Auth.Hosting;

public static class AppHost
{
    public static IDistributedApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = StrictDistributedApplication.CreateBuilder(args);
        var postgres = builder.AddPostgresContainer("concertable-auth-postgres-data");
        var authDb = postgres.AddDatabase(AuthConstants.Database);
        var asb = builder.AddServiceBus();
        asb.Topology().AddAuthTopology().RunAsEmulator();
        var migrations = builder.AddAuthMigrations<Projects.Concertable_Auth_Migrations>(authDb);
        var auth = builder.AddAuth<Projects.Concertable_Auth>(authDb, migrations, asb);
        auth.WithSpaClients([]);
        auth.WithEnvironment("ServiceAuth__AuthClientId", "concertable-auth");
        return builder;
    }
}
