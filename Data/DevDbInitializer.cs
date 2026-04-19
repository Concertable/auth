using Concertable.Auth.Services;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Auth.Data;

public class DevDbInitializer : IDbInitializer
{
    private readonly AuthDbContext _context;
    private readonly UserStore _userStore;

    public DevDbInitializer(AuthDbContext context, UserStore userStore)
    {
        _context = context;
        _userStore = userStore;
    }

    public async Task InitializeAsync()
    {
        await _context.Database.EnsureCreatedAsync();

        if (!await _context.Users.AnyAsync())
        {
            await _userStore.CreateAsync("admin@test.com", "Password1!", Role.Admin);
            await _userStore.CreateAsync("customer@test.com", "Password1!", Role.Customer);
            await _userStore.CreateAsync("artistmanager@test.com", "Password1!", Role.ArtistManager);
            await _userStore.CreateAsync("venuemanager@test.com", "Password1!", Role.VenueManager);
        }
    }
}
