using System.Net;
using Xunit.Abstractions;

namespace Concertable.Auth.IntegrationTests;

[Collection("Integration")]
public sealed class PasswordApiTests : IAsyncLifetime
{
    private const string Password = "Password123!";
    private const string NewPassword = "NewPassword123!";
    private readonly ApiFixture fixture;

    public PasswordApiTests(ApiFixture fixture, ITestOutputHelper output)
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

    #region Change password

    [Fact]
    public async Task ChangePassword_ValidCurrentPassword_UpdatesCredential()
    {
        const string email = "change@example.com";
        var credentialId = await fixture.CreateCredentialAsync(email, Password);
        var client = fixture.CreateClient(credentialId);

        var response = await client.PostAsync(
            "/Account/ChangePassword",
            Form(("CurrentPassword", Password), ("NewPassword", NewPassword)));

        await response.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Password updated.", body);
        var credential = await fixture.GetCredentialAsync(email, NewPassword);
        Assert.NotNull(credential);
        Assert.True(credential.PasswordMatches);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("incorrect")]
    public async Task ChangePassword_CredentialRefusal_RendersSameSafeError(string scenario)
    {
        var credentialId = Guid.NewGuid();
        const string email = "change-refusal@example.com";
        if (scenario == "incorrect")
            credentialId = await fixture.CreateCredentialAsync(email, Password);

        var client = fixture.CreateClient(credentialId);

        var response = await client.PostAsync(
            "/Account/ChangePassword",
            Form(("CurrentPassword", "WrongPassword123!"), ("NewPassword", NewPassword)));

        await response.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Current password is incorrect.", body);

        if (scenario == "incorrect")
        {
            var credential = await fixture.GetCredentialAsync(email, Password);
            Assert.NotNull(credential);
            Assert.True(credential.PasswordMatches);
        }
    }

    #endregion

    #region Forgot password

    [Fact]
    public async Task ForgotPassword_KnownAndUnknownEmail_RenderIdenticalResponse()
    {
        const string knownEmail = "known-reset@example.com";
        const string unknownEmail = "unknown-reset@example.com";
        var credentialId = await fixture.CreateCredentialAsync(knownEmail, Password);
        var knownClient = fixture.CreateClient();
        var unknownClient = fixture.CreateClient();

        var knownResponse = await knownClient.PostAsync(
            "/Account/ForgotPassword",
            Form(("Email", knownEmail)));
        var unknownResponse = await unknownClient.PostAsync(
            "/Account/ForgotPassword",
            Form(("Email", unknownEmail)));

        await knownResponse.ShouldBe(HttpStatusCode.OK);
        await unknownResponse.ShouldBe(HttpStatusCode.OK);
        var knownBody = await knownResponse.Content.ReadAsStringAsync();
        var unknownBody = await unknownResponse.Content.ReadAsStringAsync();
        Assert.Equal(knownBody, unknownBody);
        Assert.Contains(
            "If an account exists for that email, a password reset link has been sent.",
            knownBody);
        Assert.NotNull(await fixture.GetPasswordResetTokenAsync(credentialId));
        Assert.Single(fixture.EmailSender.Sent);
        Assert.Null(await fixture.GetCredentialAsync(unknownEmail, Password));
    }

    [Fact]
    public async Task ForgotPassword_EmailFailure_PropagatesAndDoesNotPersistToken()
    {
        const string email = "reset-email-failure@example.com";
        var credentialId = await fixture.CreateCredentialAsync(email, Password);
        var client = fixture.CreateClient();
        fixture.EmailSender.Failure = new InvalidOperationException("Email unavailable");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.PostAsync(
                "/Account/ForgotPassword",
                Form(("Email", email))));

        Assert.Null(await fixture.GetPasswordResetTokenAsync(credentialId));
    }

    #endregion

    #region Reset password

    [Fact]
    public async Task ResetPassword_ValidToken_UpdatesPasswordAndConsumesToken()
    {
        const string email = "reset@example.com";
        const string token = "valid-reset-token";
        var credentialId = await fixture.CreateCredentialAsync(email, Password);
        await fixture.AddPasswordResetTokenAsync(
            credentialId,
            token,
            DateTime.UtcNow.AddHours(1));
        var client = fixture.CreateClient();

        var response = await client.PostAsync(
            "/Account/ResetPassword",
            Form(("Token", token), ("NewPassword", NewPassword)));

        await response.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Your password has been reset.", body);
        var credential = await fixture.GetCredentialAsync(email, NewPassword);
        Assert.NotNull(credential);
        Assert.True(credential.PasswordMatches);
        Assert.Null(await fixture.GetPasswordResetTokenAsync(credentialId));

        var reusedResponse = await client.PostAsync(
            "/Account/ResetPassword",
            Form(("Token", token), ("NewPassword", Password)));
        var reusedBody = await reusedResponse.Content.ReadAsStringAsync();
        Assert.Contains("Invalid or expired reset link.", reusedBody);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("unknown")]
    [InlineData("expired")]
    [InlineData("orphaned")]
    public async Task ResetPassword_InvalidTokenVariants_RenderSameSafeFailure(string scenario)
    {
        var email = $"{scenario}-reset@example.com";
        var credentialId = await fixture.CreateCredentialAsync(email, Password);
        var token = scenario switch
        {
            "missing" => string.Empty,
            "unknown" => "unknown-reset-token",
            "expired" => "expired-reset-token",
            _ => "orphaned-reset-token"
        };

        if (scenario == "expired")
        {
            await fixture.AddPasswordResetTokenAsync(
                credentialId,
                token,
                DateTime.UtcNow.AddMinutes(-1));
        }
        else if (scenario == "orphaned")
        {
            await fixture.AddPasswordResetTokenAsync(
                Guid.NewGuid(),
                token,
                DateTime.UtcNow.AddHours(1));
        }

        var client = fixture.CreateClient();

        var response = await client.PostAsync(
            "/Account/ResetPassword",
            Form(("Token", token), ("NewPassword", NewPassword)));

        await response.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid or expired reset link.", body);
        var credential = await fixture.GetCredentialAsync(email, Password);
        Assert.NotNull(credential);
        Assert.True(credential.PasswordMatches);
    }

    #endregion

    [Fact]
    public async Task Login_CancelledDatabaseOperation_PropagatesCancellation()
    {
        const string email = "cancelled@example.com";
        await fixture.CreateCredentialAsync(email, Password);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fixture.InvokeAuthServiceAsync(async service =>
            {
                _ = await service.LoginAsync(email, Password, cancellation.Token);
            }));
    }

    private static FormUrlEncodedContent Form(params (string Name, string Value)[] fields) =>
        new(fields.Select(field => new KeyValuePair<string, string>(field.Name, field.Value)));
}
