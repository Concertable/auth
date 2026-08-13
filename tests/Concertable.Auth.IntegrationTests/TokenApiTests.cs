using System.Net;
using System.Text.Json;
using Xunit.Abstractions;

namespace Concertable.Auth.IntegrationTests;

[Collection("E2EToken")]
public sealed class TokenApiTests : IAsyncLifetime
{
    private const string Password = "Password123!";
    private readonly E2ETokenApiFixture fixture;

    public TokenApiTests(E2ETokenApiFixture fixture, ITestOutputHelper output)
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
