using Concertable.Auth.Migrations;

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__AuthDb")
    ?? throw new InvalidOperationException(
        "Connection string 'ConnectionStrings__AuthDb' is required for the Auth migration job.");

await AuthMigrationJob.RunAsync(connectionString).ConfigureAwait(false);
