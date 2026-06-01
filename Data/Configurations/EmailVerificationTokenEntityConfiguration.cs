using Concertable.Auth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Auth.Data.Configurations;

internal sealed class EmailVerificationTokenEntityConfiguration : IEntityTypeConfiguration<EmailVerificationTokenEntity>
{
    public void Configure(EntityTypeBuilder<EmailVerificationTokenEntity> builder)
    {
        builder.ToTable(Schema.Tables.EmailVerificationTokens, Schema.Name);
        builder.HasIndex(t => t.CredentialId);
        builder.HasIndex(t => t.Token).IsUnique();
    }
}
