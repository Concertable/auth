using System.Net;
using Xunit.Abstractions;

namespace Concertable.Auth.IntegrationTests;

[Collection("Integration")]
public sealed class LogoutApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public LogoutApiTests(ApiFixture fixture, ITestOutputHelper output)
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
    public async Task LogoutService_ValidContext_ReturnsSomeRedirect()
    {
        const string expectedRedirect = "https://localhost:5174";
        var logoutId = await fixture.CreateLogoutContextAsync(expectedRedirect);

        var result = await fixture.InvokeAuthServiceAsync(
            service => service.LogoutAsync(logoutId));

        Assert.True(result.TryGetValue(out var redirect));
        Assert.Equal(expectedRedirect, redirect);
    }

    [Fact]
    public async Task LogoutService_MissingContext_ReturnsNone()
    {
        var result = await fixture.InvokeAuthServiceAsync(
            service => service.LogoutAsync(null));

        Assert.True(result.IsNone);
    }

    #endregion

    #region Razor logout

    [Fact]
    public async Task Logout_GetWithoutContext_RendersConfirmation()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/Account/Logout");

        await response.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Are you sure you want to sign out?", body);
    }

    [Fact]
    public async Task Logout_PostWithoutContext_SignsOutAndRedirectsToRoot()
    {
        var client = fixture.CreateClient();

        var response = await client.PostAsync(
            "/Account/Logout",
            new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>()));

        await response.ShouldBe(HttpStatusCode.Redirect);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Logout_ValidContext_RedirectsToClientAfterSignout()
    {
        var client = fixture.CreateClient();
        var logoutId = await fixture.CreateLogoutContextAsync("https://localhost:5174");

        var response = await client.PostAsync(
            $"/Account/Logout?logoutId={Uri.EscapeDataString(logoutId)}",
            new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>()));

        await response.ShouldBe(HttpStatusCode.Redirect);
        Assert.Equal("https://localhost:5174", response.Headers.Location?.OriginalString);
    }

    #endregion
}
