using Concertable.DataAccess.Application;
using Concertable.Seed.Shared;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Concertable.Auth;

internal sealed class AuthDbInitializer : IDbInitializer
{
    private readonly IEnumerable<IDevSeeder> seeders;
    private readonly ILogger<AuthDbInitializer> logger;

    public AuthDbInitializer(
        IEnumerable<IDevSeeder> seeders,
        ILogger<AuthDbInitializer> logger)
    {
        this.seeders = seeders;
        this.logger = logger;
    }

    public async Task InitializeAsync()
    {
        var ordered = seeders.OrderBy(s => s.Order).ToList();
        if (ordered.Count == 0) return;

        logger.BeginDbInitialization(ordered.Count);
        var total = Stopwatch.StartNew();

        foreach (var seeder in ordered)
        {
            var name = seeder.GetType().Name;
            logger.SeedingSeeder(name, seeder.Order);
            var sw = Stopwatch.StartNew();
            try
            {
                await seeder.MigrateAsync();
                await seeder.SeedAsync();
                sw.Stop();
                logger.SeederCompleted(name, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                logger.SeederFailed(name, sw.ElapsedMilliseconds, ex);
                throw;
            }
        }

        total.Stop();
        logger.DbInitializationComplete(total.ElapsedMilliseconds);
    }
}
