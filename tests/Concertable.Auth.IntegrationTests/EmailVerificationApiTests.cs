using System.Net;
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

        switch (scenario)
        {
            case "unknown":
                requestPath += "?token=unknown-verification-token";
                break;
            case "expired":
                await fixture.AddEmailVerificationTokenAsync(
                    credentialId,
                    "expired-verification-token",
                    DateTime.UtcNow.AddMinutes(-1));
                requestPath += "?token=expired-verification-token";
                break;
            case "orphaned":
                await fixture.AddEmailVerificationTokenAsync(
                    Guid.NewGuid(),
                    "orphaned-verification-token",
                    DateTime.UtcNow.AddHours(1));
                requestPath += "?token=orphaned-verification-token";
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
    }
}
