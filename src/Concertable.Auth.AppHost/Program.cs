using Concertable.B2B.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServerContainer("concertable-auth-sql-data");
var authDb = sql.AddDatabase(AuthConstants.Database);
var b2bDb = sql.AddDatabase(B2BConstants.Database);

var asb = builder.AddServiceBus();
asb.Topology().AddAuthTopology();

var auth = builder.AddAuth<Projects.Concertable_Auth>(authDb, b2bDb, asb);
auth.WithEnvironment("ServiceAuth__AuthClientId", "concertable-auth");

builder.Build().Run();
