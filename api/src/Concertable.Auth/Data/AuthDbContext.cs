using Concertable.Auth.Data.Entities;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.Auth.Data;

internal sealed class AuthDbContext(
    DbContextOptions<AuthDbContext> options,
    IOptions<OutboxOptions> outboxOptions,
    AuthConfigurationProvider provider)
    : DbContextBase(options, outboxOptions)
{
    public DbSet<CredentialEntity> Credentials => Set<CredentialEntity>();
    public DbSet<EmailVerificationTokenEntity> EmailVerificationTokens => Set<EmailVerificationTokenEntity>();
    public DbSet<PasswordResetTokenEntity> PasswordResetTokens => Set<PasswordResetTokenEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema.Name);
        provider.Configure(modelBuilder);
    }
}
