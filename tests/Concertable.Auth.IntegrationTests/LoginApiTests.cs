using System.Net;
using System.Text.Json;
using Xunit.Abstractions;

namespace Concertable.Auth.IntegrationTests;

[Collection("Integration")]
public sealed class LoginApiTests : IAsyncLifetime
{
    private const string Password = "Password123!";
    private readonly ApiFixture fixture;

    public LoginApiTests(ApiFixture fixture, ITestOutputHelper output)
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

    #region Razor login

    [Fact]
    public async Task Login_ValidCredential_SignsInAndRedirects()
    {
        const string email = "verified@example.com";
        await fixture.CreateCredentialAsync(email, Password);
        var client = fixture.CreateClient();
        var content = Form(
            ("Email", email),
            ("Password", Password),
            ("ReturnUrl", "/after-login"));

        var response = await client.PostAsync("/Account/Login", content);

        await response.ShouldBe(HttpStatusCode.Redirect);
        Assert.Equal("/after-login", response.Headers.Location?.OriginalString);
        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            value => value.Contains("idsrv", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("wrong-password")]
    [InlineData("unverified")]
    public async Task Login_AuthenticationMiss_RendersSameSafeError(string scenario)
    {
        var (email, password) = await ArrangeAuthenticationMissAsync(scenario);
        var client = fixture.CreateClient();
        var content = Form(("Email", email), ("Password", password));

        var response = await client.PostAsync("/Account/Login", content);

        await response.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid email or password.", body);
    }

    #endregion

    #region Password grant

    [Fact]
    public async Task Token_ValidCredential_ReturnsAccessToken()
    {
        const string email = "token@example.com";
        await fixture.CreateCredentialAsync(email, Password);
        var client = fixture.CreateClient();

        var response = await client.PostAsync(
            "/connect/token",
            TokenRequest(email, Password));

        await response.ShouldBe(HttpStatusCode.OK);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(payload.RootElement.GetProperty("access_token").GetString()));
        Assert.Equal("Bearer", payload.RootElement.GetProperty("token_type").GetString());
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("wrong-password")]
    [InlineData("unverified")]
    public async Task Token_AuthenticationMiss_ReturnsSameInvalidGrant(string scenario)
    {
        var (email, password) = await ArrangeAuthenticationMissAsync(scenario);
        var client = fixture.CreateClient();

        var response = await client.PostAsync(
            "/connect/token",
            TokenRequest(email, password));

        await response.ShouldBe(HttpStatusCode.BadRequest);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("invalid_grant", payload.RootElement.GetProperty("error").GetString());
        Assert.Equal("Invalid credentials", payload.RootElement.GetProperty("error_description").GetString());
    }

    #endregion

    private async Task<(string Email, string Password)> ArrangeAuthenticationMissAsync(string scenario)
    {
        var email = $"{scenario}@example.com";
        switch (scenario)
        {
            case "wrong-password":
                await fixture.CreateCredentialAsync(email, Password);
                return (email, "WrongPassword123!");
            case "unverified":
                await fixture.CreateCredentialAsync(email, Password, verified: false);
                return (email, Password);
            default:
                return (email, Password);
        }
    }

    private static FormUrlEncodedContent TokenRequest(string email, string password) =>
        Form(
            ("grant_type", "password"),
            ("client_id", "concertable-test"),
            ("username", email),
            ("password", password),
            ("scope", "openid"));

    private static FormUrlEncodedContent Form(params (string Name, string Value)[] fields) =>
        new(fields.Select(field => new KeyValuePair<string, string>(field.Name, field.Value)));
}
