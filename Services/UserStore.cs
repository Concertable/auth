using Concertable.Auth.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Auth.Services;

public class UserStore
{
    private readonly AuthDbContext _db;

    public UserStore(AuthDbContext db)
    {
        _db = db;
    }

    public Task<User?> FindByEmailAsync(string email) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> FindByIdAsync(Guid id) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public bool ValidatePassword(User user, string password) =>
        BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

    public async Task CreateAsync(string email, string password, Role role)
    {
        _db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role
        });
        await _db.SaveChangesAsync();
    }
}
