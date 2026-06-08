using Concertable.Auth.Data.Configurations;
using Concertable.DataAccess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Auth.Data;

internal sealed class AuthConfigurationProvider : IEntityTypeConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CredentialEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EmailVerificationTokenEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PasswordResetTokenEntityConfiguration());
    }
}
