using Concertable.Auth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Auth.Data.Configurations;

internal sealed class CredentialEntityConfiguration : IEntityTypeConfiguration<CredentialEntity>
{
    public void Configure(EntityTypeBuilder<CredentialEntity> builder)
    {
        builder.ToTable("Credentials", "auth");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Email).IsRequired().HasMaxLength(256);
        builder.HasIndex(c => c.Email).IsUnique();
        builder.Property(c => c.PasswordHash).IsRequired();
    }
}
