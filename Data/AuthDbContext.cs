using Concertable.Auth.Data.Entities;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Domain;
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
        provider.Configure(modelBuilder);

        modelBuilder.Entity<OutboxMessageEntity>(b =>
        {
            b.ToTable("Outbox", "messaging", t => t.ExcludeFromMigrations());
            b.Property(m => m.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<InboxMessageEntity>(b =>
        {
            b.ToTable("Inbox", "messaging", t => t.ExcludeFromMigrations());
            b.HasKey(m => new { m.MessageId, m.ConsumerName });
            b.Property(m => m.MessageId).ValueGeneratedNever();
            b.Property(m => m.ConsumerName).IsRequired().HasMaxLength(256);
            b.Property(m => m.MessageType).IsRequired().HasColumnType("nvarchar(450)");
            b.Property(m => m.ReceivedAt).IsRequired();
        });
    }
}
