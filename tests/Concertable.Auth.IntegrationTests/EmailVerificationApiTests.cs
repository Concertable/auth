using System.Net;
using Concertable.Auth.Domain;
using Xunit.Abstractions;

namespace Concertable.Auth.IntegrationTests;

[Collection("Integration")]
public sealed class EmailVerificationApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public EmailVerificationApiTests(ApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync()
    {
        fixture.DetachOutput();
        return Task.CompletedTask;
    }

    #region Auth service

    [Fact]
    public async Task VerifyEmailService_ValidToken_ReturnsSuccess()
    {
        const string token = "service-valid-verification-token";
        var credentialId = await fixture.CreateCredentialAsync(
            "service-verify@example.com",
            "Password123!",
            verified: false);
        await fixture.AddEmailVerificationTokenAsync(
            credentialId,
            token,
            DateTime.UtcNow.AddHours(1));

        var result = await fixture.InvokeAuthServiceAsync(
            service => service.VerifyEmailAsync(token));

        Assert.True(result.IsSuccess);
        Assert.Null(await fixture.GetEmailVerificationTokenAsync(credentialId));
    }

    [Fact]
    public async Task VerifyEmailService_InvalidToken_ReturnsOwnedFailure()
    {
        var result = await fixture.InvokeAuthServiceAsync(
            service => service.VerifyEmailAsync("unknown-verification-token"));

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<VerifyEmailError.InvalidOrExpiredToken>(error);
    }

    [Fact]
    public async Task VerifyEmailService_CancelledDatabaseOperation_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fixture.InvokeAuthServiceAsync(async service =>
            {
                _ = await service.VerifyEmailAsync(
                    "cancelled-verification-token",
                    cancellation.Token);
            }));
    }

    #endregion

    #region Razor verification

    [Fact]
    public async Task VerifyEmail_ValidToken_VerifiesCredentialAndConsumesToken()
    {
        const string email = "verify@example.com";
        const string token = "valid-verification-token";
        var credentialId = await fixture.CreateCredentialAsync(email, "Password123!", verified: false);
        await fixture.AddEmailVerificationTokenAsync(
            credentialId,
            token,
            DateTime.UtcNow.AddHours(1));
        var client = fixture.CreateClient();

        var response = await client.GetAsync($"/Account/VerifyEmail?token={token}");

        await response.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Your email has been verified.", body);
        var credential = await fixture.GetCredentialAsync(email, "Password123!");
        Assert.NotNull(credential);
        Assert.True(credential.IsEmailVerified);
        Assert.Null(await fixture.GetEmailVerificationTokenAsync(credentialId));

        var reusedResponse = await client.GetAsync($"/Account/VerifyEmail?token={token}");
        var reusedBody = await reusedResponse.Content.ReadAsStringAsync();
        Assert.Contains("This verification link is invalid or has expired.", reusedBody);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("unknown")]
    [InlineData("expired")]
    [InlineData("orphaned")]
    public async Task VerifyEmail_InvalidTokenVariants_RenderSameSafeFailure(string scenario)
    {
        var email = $"{scenario}@example.com";
        var credentialId = await fixture.CreateCredentialAsync(
            email,
            "Password123!",
            verified: false);
        var requestPath = "/Account/VerifyEmail";
        string? storedToken = null;

        switch (scenario)
        {
            case "unknown":
                requestPath += "?token=unknown-verification-token";
                break;
            case "expired":
                storedToken = "expired-verification-token";
                await fixture.AddEmailVerificationTokenAsync(
                    credentialId,
                    storedToken,
                    DateTime.UtcNow.AddMinutes(-1));
                requestPath += $"?token={storedToken}";
                break;
            case "orphaned":
                storedToken = "orphaned-verification-token";
                await fixture.AddEmailVerificationTokenAsync(
                    Guid.NewGuid(),
                    storedToken,
                    DateTime.UtcNow.AddHours(1));
                requestPath += $"?token={storedToken}";
                break;
        }

        var client = fixture.CreateClient();

        var response = await client.GetAsync(requestPath);

        await response.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("This verification link is invalid or has expired.", body);
        var credential = await fixture.GetCredentialAsync(email, "Password123!");
        Assert.NotNull(credential);
        Assert.False(credential.IsEmailVerified);
        if (storedToken is not null)
            Assert.True(await fixture.EmailVerificationTokenExistsAsync(storedToken));
    }

    #endregion
}
