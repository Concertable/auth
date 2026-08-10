using Concertable.Auth.Data;
using Concertable.Auth.Data.Entities;
using Concertable.Auth.Domain;
using Concertable.Auth.Services;
using Concertable.DataAccess.Application;
using Concertable.Messaging.Contracts;
using Concertable.Seed.Shared;
using Concertable.Shared.Email.Application;
using Concertable.Testing;
using Concertable.Testing.Integration;
using Concertable.Testing.Integration.Logging;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.Auth.IntegrationTests.Fixtures;

public sealed record CredentialState(Guid Id, bool IsEmailVerified, bool PasswordMatches);

public sealed class ApiFixture : IAsyncLifetime
{
    private readonly XunitOutputAccessor outputAccessor = new();
    private readonly Dictionary<string, string?> previousEnvironment = new();
    private SqlFixture sqlFixture = null!;
    private WebApplicationFactory<Program> factory = null!;

    public TestEmailSender EmailSender { get; } = new();

    public void AttachOutput(ITestOutputHelper output) => outputAccessor.Output = output;
    public void DetachOutput() => outputAccessor.Output = null;

    public async Task InitializeAsync()
    {
        sqlFixture = new SqlFixture();
        await sqlFixture.InitializeAsync();
        ConfigureEnvironment();

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("E2E");
            builder.ConfigureTestServices(services =>
            {
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(new XunitLoggerProvider(outputAccessor));
                    logging.SetMinimumLevel(LogLevel.Information);
                });

                var receivers = services
                    .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                        && descriptor.ImplementationType?.Name == "AzureServiceBusReceiver")
                    .ToList();
                foreach (var receiver in receivers)
                    services.Remove(receiver);

                services.RemoveAll<IDevSeeder>();
                services.Replace(ServiceDescriptor.Singleton<IBusTransport, TestBusTransport>());
                services.Replace(ServiceDescriptor.Singleton<IEmailSender>(EmailSender));

                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultScheme = TestAuthHandler.SchemeName;
                });
                services.AddAuthentication()
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName,
                        _ => { });

                services.PostConfigure<RazorPagesOptions>(options =>
                    options.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute()));
            });
        });

        try
        {
            _ = factory.Services;
        }
        finally
        {
            RestoreEnvironment();
        }

        await sqlFixture.InitializeRespawnerAsync();
    }

    public async Task ResetAsync()
    {
        await sqlFixture.ResetAsync();
        EmailSender.Reset();
    }

    public async Task DisposeAsync()
    {
        await factory.DisposeAsync();
        await sqlFixture.DisposeAsync();
    }

    public HttpClient CreateClient(Guid? userId = null)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        if (userId.HasValue)
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.Value.ToString());

        return client;
    }

    public string CreateAuthorizationReturnUrl() =>
        "/connect/authorize/callback?client_id=customer-web"
        + "&redirect_uri=https%3A%2F%2Flocalhost%3A5174%2Fauth%2Fcallback"
        + "&response_type=code&scope=openid&state=test-state&nonce=test-nonce"
        + "&code_challenge=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "&code_challenge_method=S256";

    public async Task<Guid> CreateCredentialAsync(
        string email,
        string password,
        bool verified = true)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var credential = CredentialEntity.Create(email, hasher.Hash(password), "customer-web");
        credential.ClearDomainEvents();
        if (verified)
            credential.VerifyEmail();

        context.Credentials.Add(credential);
        await context.SaveChangesAsync();
        return credential.Id;
    }

    public async Task<CredentialState?> GetCredentialAsync(string email, string password)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var credential = await context.Credentials
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Email == email);

        return credential is null
            ? null
            : new CredentialState(
                credential.Id,
                credential.IsEmailVerified,
                hasher.Verify(password, credential.PasswordHash));
    }

    public async Task<int> CountCredentialsAsync(string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return await context.Credentials.CountAsync(credential => credential.Email == email);
    }

    public async Task AddEmailVerificationTokenAsync(
        Guid credentialId,
        string token,
        DateTime expires)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        context.EmailVerificationTokens.Add(
            EmailVerificationTokenEntity.Create(credentialId, token, expires));
        await context.SaveChangesAsync();
    }

    public async Task AddPasswordResetTokenAsync(
        Guid credentialId,
        string token,
        DateTime expires)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        context.PasswordResetTokens.Add(
            PasswordResetTokenEntity.Create(credentialId, token, expires));
        await context.SaveChangesAsync();
    }

    public async Task<string?> GetEmailVerificationTokenAsync(Guid credentialId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return await context.EmailVerificationTokens
            .Where(token => token.CredentialId == credentialId)
            .Select(token => token.Token)
            .SingleOrDefaultAsync();
    }

    public async Task<bool> EmailVerificationTokenExistsAsync(string token)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return await context.EmailVerificationTokens.AnyAsync(candidate => candidate.Token == token);
    }

    public async Task<string?> GetPasswordResetTokenAsync(Guid credentialId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return await context.PasswordResetTokens
            .Where(token => token.CredentialId == credentialId)
            .Select(token => token.Token)
            .SingleOrDefaultAsync();
    }

    public async Task<bool> PasswordResetTokenExistsAsync(string token)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return await context.PasswordResetTokens.AnyAsync(candidate => candidate.Token == token);
    }

    public async Task InvokeAuthServiceAsync(Func<IAuthService, Task> action)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<IAuthService>());
    }

    public async Task<T> InvokeAuthServiceAsync<T>(Func<IAuthService, Task<T>> action)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var previousContext = accessor.HttpContext;
        accessor.HttpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };

        try
        {
            return await action(scope.ServiceProvider.GetRequiredService<IAuthService>());
        }
        finally
        {
            accessor.HttpContext = previousContext;
        }
    }

    public async Task<string> CreateLogoutContextAsync(string postLogoutRedirectUri)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IMessageStore<LogoutMessage>>();
        var message = new LogoutMessage
        {
            ClientId = "customer-web",
            PostLogoutRedirectUri = postLogoutRedirectUri
        };
        return await store.WriteAsync(new Message<LogoutMessage>(message, DateTime.UtcNow));
    }

    private void ConfigureEnvironment()
    {
        SetEnvironment("DOTNET_ENVIRONMENT", "E2E");
        SetEnvironment("ASPNETCORE_ENVIRONMENT", "E2E");
        SetEnvironment("ConnectionStrings__AuthDb", sqlFixture.ConnectionString);
        SetEnvironment("ConnectionStrings__B2BDb", sqlFixture.ConnectionString);
        SetEnvironment(
            "ConnectionStrings__asb",
            "Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=test");
        SetEnvironment("Auth__Authority", "https://localhost");
        SetEnvironment("ServiceAuth__AuthClientId", "concertable-auth");
        SetEnvironment("ServiceAuth__B2BClientSecret", "b2b-test-secret");
        SetEnvironment("ServiceAuth__CustomerClientSecret", "customer-test-secret");
        SetEnvironment("ServiceAuth__AuthClientSecret", "auth-test-secret");
    }

    private void SetEnvironment(string name, string value)
    {
        previousEnvironment[name] = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
    }

    private void RestoreEnvironment()
    {
        foreach (var variable in previousEnvironment)
            Environment.SetEnvironmentVariable(variable.Key, variable.Value);
    }
}
