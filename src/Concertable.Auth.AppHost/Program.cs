var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServerContainer("concertable-auth-sql-data");
var authDb = sql.AddDatabase(AppHostConstants.Databases.Auth);
var b2bDb = sql.AddDatabase(AppHostConstants.Databases.B2B);

var asb = builder.AddServiceBus();

var auth = builder.AddAuth<Projects.Concertable_Auth>(authDb, b2bDb, asb);
auth.WithEnvironment("ServiceAuth__AuthClientId", "concertable-auth");

builder.Build().Run();
