using Concertable.Auth.Data.Entities;
using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Auth.Data;

internal class AuthDbContext(
    DbContextOptions<AuthDbContext> options,
    AuthConfigurationProvider provider)
    : DbContextBase(options)
{
    public DbSet<CredentialEntity> Credentials => Set<CredentialEntity>();
    public DbSet<EmailVerificationTokenEntity> EmailVerificationTokens => Set<EmailVerificationTokenEntity>();
    public DbSet<PasswordResetTokenEntity> PasswordResetTokens => Set<PasswordResetTokenEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        provider.Configure(modelBuilder);
    }
}
