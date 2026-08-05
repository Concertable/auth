using System.Net;
using Xunit.Abstractions;

namespace Concertable.Auth.IntegrationTests;

[Collection("Integration")]
public sealed class RegistrationApiTests : IAsyncLifetime
{
    private const string Password = "Password123!";
    private readonly ApiFixture fixture;

    public RegistrationApiTests(ApiFixture fixture, ITestOutputHelper output)
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
    public async Task Register_ValidRequest_CreatesCredentialAndVerification()
    {
        const string email = "new@example.com";
        var client = fixture.CreateClient();
        var returnUrl = fixture.CreateAuthorizationReturnUrl();

        var response = await client.PostAsync(
            "/Account/Register",
            Form(("Email", email), ("Password", Password), ("ReturnUrl", returnUrl)));

        await response.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Account created. Check your email for a verification link.", body);
        Assert.Equal(1, await fixture.CountCredentialsAsync(email));

        var credential = await fixture.GetCredentialAsync(email, Password);
        Assert.NotNull(credential);
        Assert.False(credential.IsEmailVerified);
        var emailMessage = Assert.Single(fixture.EmailSender.Sent);
        Assert.Equal(email, emailMessage.To);
        Assert.Equal(emailMessage.Token, await fixture.GetEmailVerificationTokenAsync(credential.Id));
    }

    [Fact]
    public async Task Register_DuplicateEmail_DisclosesConflictWithoutSideEffects()
    {
        const string email = "duplicate@example.com";
        await fixture.CreateCredentialAsync(email, Password);
        var client = fixture.CreateClient();
        var returnUrl = fixture.CreateAuthorizationReturnUrl();

        var response = await client.PostAsync(
            "/Account/Register",
            Form(("Email", email), ("Password", Password), ("ReturnUrl", returnUrl)));

        await response.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("An account with that email already exists.", body);
        Assert.Equal(1, await fixture.CountCredentialsAsync(email));
        Assert.Empty(fixture.EmailSender.Sent);
    }

    [Fact]
    public async Task Register_MissingAuthorizationContext_RejectsRequest()
    {
        const string email = "no-context@example.com";
        var client = fixture.CreateClient();

        var response = await client.PostAsync(
            "/Account/Register",
            Form(("Email", email), ("Password", Password), ("ReturnUrl", string.Empty)));

        await response.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Sign up must be initiated from a Concertable surface.", body);
        Assert.Equal(0, await fixture.CountCredentialsAsync(email));
        Assert.Empty(fixture.EmailSender.Sent);
    }

    [Fact]
    public async Task Register_EmailFailure_PropagatesAndDoesNotPersistToken()
    {
        const string email = "email-failure@example.com";
        var client = fixture.CreateClient();
        var returnUrl = fixture.CreateAuthorizationReturnUrl();
        fixture.EmailSender.Failure = new InvalidOperationException("Email unavailable");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.PostAsync(
                "/Account/Register",
                Form(("Email", email), ("Password", Password), ("ReturnUrl", returnUrl))));

        var credential = await fixture.GetCredentialAsync(email, Password);
        Assert.NotNull(credential);
        Assert.Null(await fixture.GetEmailVerificationTokenAsync(credential.Id));
    }

    private static FormUrlEncodedContent Form(params (string Name, string Value)[] fields) =>
        new(fields.Select(field => new KeyValuePair<string, string>(field.Name, field.Value)));
}
