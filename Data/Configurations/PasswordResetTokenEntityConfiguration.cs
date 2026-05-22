using Concertable.Auth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Auth.Data.Configurations;

internal sealed class PasswordResetTokenEntityConfiguration : IEntityTypeConfiguration<PasswordResetTokenEntity>
{
    public void Configure(EntityTypeBuilder<PasswordResetTokenEntity> builder)
    {
        builder.ToTable("PasswordResetTokens", "auth");
        builder.HasIndex(t => t.CredentialId);
        builder.HasIndex(t => t.Token).IsUnique();
    }
}
