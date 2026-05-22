using Concertable.Auth.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Auth.Data.Seeders;

internal static class SeedIds
{
    public static readonly Guid Admin = new("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid Customer1 = new("c0000000-0000-0000-0000-000000000001");
    public static readonly Guid Customer2 = new("c0000000-0000-0000-0000-000000000002");
    public static readonly Guid Customer3 = new("c0000000-0000-0000-0000-000000000003");

    public static Guid ArtistManager(int n) => new($"a1000000-0000-0000-0000-{n:D12}");
    public static Guid VenueManager(int n) => new($"b1000000-0000-0000-0000-{n:D12}");
}

internal sealed class AuthDevSeeder
{
    private readonly AuthDbContext context;

    public AuthDevSeeder(AuthDbContext context)
    {
        this.context = context;
    }

    public async Task SeedAsync(string passwordHash, CancellationToken ct = default)
    {
        if (await context.Credentials.AnyAsync(ct))
            return;

        context.Credentials.Add(CredentialEntity.Seed(SeedIds.Admin, "admin@test.com", passwordHash));
        context.Credentials.Add(CredentialEntity.Seed(SeedIds.Customer1, "customer1@test.com", passwordHash));
        context.Credentials.Add(CredentialEntity.Seed(SeedIds.Customer2, "customer2@test.com", passwordHash));
        context.Credentials.Add(CredentialEntity.Seed(SeedIds.Customer3, "customer3@test.com", passwordHash));

        for (int i = 1; i <= 35; i++)
            context.Credentials.Add(CredentialEntity.Seed(SeedIds.ArtistManager(i), $"artistmanager{i}@test.com", passwordHash));

        for (int i = 1; i <= 35; i++)
            context.Credentials.Add(CredentialEntity.Seed(SeedIds.VenueManager(i), $"venuemanager{i}@test.com", passwordHash));

        await context.SaveChangesAsync(ct);
    }
}
