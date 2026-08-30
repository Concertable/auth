using Concertable.Auth.OperationalStoreMigration;

return await RunAsync(args);

static async Task<int> RunAsync(string[] args)
{
    const string confirmation = "COPY_IDSERV_B2BDB_TO_AUTHDB";

    if (args.Contains("--help", StringComparer.Ordinal))
    {
        Console.WriteLine("Copies Duende operational-store rows from B2BDb to AuthDb.");
        Console.WriteLine();
        Console.WriteLine("Required environment variables:");
        Console.WriteLine("  ConnectionStrings__B2BDb  Source connection string");
        Console.WriteLine("  ConnectionStrings__AuthDb Target connection string");
        Console.WriteLine();
        Console.WriteLine("Dry run (default):");
        Console.WriteLine("  dotnet run --project Concertable.Auth.OperationalStoreMigration");
        Console.WriteLine();
        Console.WriteLine("Execute after Auth traffic is quiesced and the idsrv schema exists in AuthDb:");
        Console.WriteLine($"  dotnet run --project Concertable.Auth.OperationalStoreMigration -- --execute --confirm {confirmation}");
        return 0;
    }

    var execute = args.Contains("--execute", StringComparer.Ordinal);
    var dryRun = args.Length == 0 || args.SequenceEqual(["--dry-run"], StringComparer.Ordinal);
    var confirmed = args.Length == 3
        && args[0] == "--execute"
        && args[1] == "--confirm"
        && args[2] == confirmation;

    if ((!execute && !dryRun) || (execute && !confirmed))
    {
        Console.Error.WriteLine("Invalid arguments. Pass --help for usage. Execute mode requires the exact confirmation phrase.");
        return 2;
    }

    var sourceConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__B2BDb");
    var targetConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__AuthDb");
    if (string.IsNullOrWhiteSpace(sourceConnectionString) || string.IsNullOrWhiteSpace(targetConnectionString))
    {
        Console.Error.WriteLine("ConnectionStrings__B2BDb and ConnectionStrings__AuthDb are required.");
        return 2;
    }

    try
    {
        var migrator = new OperationalStoreMigrator();
        var report = execute
            ? await migrator.CopyAsync(sourceConnectionString, targetConnectionString)
            : await migrator.InspectAsync(sourceConnectionString, targetConnectionString);

        Console.WriteLine(execute ? "Operational-store copy committed." : "Operational-store dry run; no data changed.");
        foreach (var table in report.Tables)
        {
            Console.WriteLine(
                $"idsrv.{table.Name}: source={table.SourceRows} ({table.SourceSha256}), "
                + $"target={table.TargetRows} ({table.TargetSha256})");
        }

        if (report.Warning is not null)
            Console.Error.WriteLine($"WARNING: {report.Warning}");

        if (!execute)
            Console.WriteLine(report.TargetIsEmpty
                ? "Target is empty and schema-compatible; the copy can proceed after Auth traffic is quiesced."
                : "Target is not empty; execute mode will refuse to run.");

        return 0;
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"Operational-store migration failed: {exception.Message}");
        return 1;
    }
}
