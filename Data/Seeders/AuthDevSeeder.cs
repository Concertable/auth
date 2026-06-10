using Concertable.Auth.Contracts;
using Concertable.Auth.Data.Entities;
using Concertable.Auth.Data.Factories;
using Concertable.Auth.Services;
using Concertable.Seed.Shared;
using Concertable.Seed.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.Auth.Data.Seeders;

internal sealed class AuthDevSeeder : IDevSeeder
{
    public int Order => 0;

    private const string DefaultPassword = "Password11!";

    private readonly AuthDbContext context;
    private readonly IPasswordHasher passwordHasher;
    private readonly ILogger<AuthDevSeeder> logger;

    public AuthDevSeeder(AuthDbContext context, IPasswordHasher passwordHasher, ILogger<AuthDevSeeder> logger)
    {
        this.context = context;
        this.passwordHasher = passwordHasher;
        this.logger = logger;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var existing = await context.Credentials.CountAsync(ct);
        if (existing > 0)
        {
            logger.SeedSkipped();
            return;
        }

        var passwordHash = passwordHasher.Hash(DefaultPassword);

        var toAdd = new List<CredentialEntity>
        {
            CredentialFactory.Create(SeedUsers.Admin, SeedUsers.AdminEmail, passwordHash, ClientIds.Admin)
        };

        for (int i = 1; i <= SeedCustomers.CustomerCount; i++)
            toAdd.Add(CredentialFactory.Create(
                SeedCustomers.CustomerId(i), SeedCustomers.CustomerEmail(i), passwordHash, ClientIds.CustomerWeb));

        foreach (var m in SeedUsers.Managers)
            toAdd.Add(CredentialFactory.Create(
                m.Id, m.Email, passwordHash,
                m.Kind == ManagerKind.Artist ? ClientIds.ArtistWeb : ClientIds.VenueWeb));

        logger.SeedingCredentials(existing, toAdd.Count);
        context.Credentials.AddRange(toAdd);
        await context.SaveChangesAsync(ct);
        logger.SeededCredentials(toAdd.Count);
    }
}
