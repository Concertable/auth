using Concertable.Auth.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Auth.Data.Seeders;

internal sealed class AuthTestSeeder
{
    private readonly AuthDbContext context;

    public AuthTestSeeder(AuthDbContext context)
    {
        this.context = context;
    }

    public async Task SeedAsync(string passwordHash, CancellationToken ct = default)
    {
        if (await context.Credentials.AnyAsync(ct))
            return;

        context.Credentials.Add(CredentialEntity.Seed(SeedIds.Admin, "admin@test.com", passwordHash));
        context.Credentials.Add(CredentialEntity.Seed(SeedIds.Customer1, "customer1@test.com", passwordHash));
        context.Credentials.Add(CredentialEntity.Seed(SeedIds.ArtistManager(1), "artistmanager1@test.com", passwordHash));
        context.Credentials.Add(CredentialEntity.Seed(SeedIds.ArtistManager(2), "artistmanager2@test.com", passwordHash));
        context.Credentials.Add(CredentialEntity.Seed(SeedIds.VenueManager(1), "venuemanager1@test.com", passwordHash));
        context.Credentials.Add(CredentialEntity.Seed(SeedIds.VenueManager(2), "venuemanager2@test.com", passwordHash));

        await context.SaveChangesAsync(ct);
    }
}
