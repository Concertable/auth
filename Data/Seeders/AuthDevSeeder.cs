using Concertable.Auth.Contracts;
using Concertable.Auth.Data.Entities;
using Concertable.Auth.Data.Factories;
using Concertable.Auth.Services;
using Concertable.Seeding;
using Concertable.Seeding.Identity;
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

        foreach (var customer in SeedCustomers.All)
            toAdd.Add(CredentialFactory.Create(customer.Id, customer.Email, passwordHash, ClientIds.CustomerWeb));

        for (int i = 1; i <= SeedUsers.ManagerCount; i++)
            toAdd.Add(CredentialFactory.Create(
                SeedUsers.ArtistManagerId(i), SeedUsers.ArtistManagerEmail(i), passwordHash, ClientIds.ArtistWeb));

        for (int i = 1; i <= SeedUsers.ManagerCount; i++)
            toAdd.Add(CredentialFactory.Create(
                SeedUsers.VenueManagerId(i), SeedUsers.VenueManagerEmail(i), passwordHash, ClientIds.VenueWeb));

        logger.SeedingCredentials(existing, toAdd.Count);
        context.Credentials.AddRange(toAdd);
        await context.SaveChangesAsync(ct);
        logger.SeededCredentials(toAdd.Count);
    }
}
