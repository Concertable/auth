using Concertable.Auth.Contracts;

namespace Concertable.Auth.UnitTests;

public sealed class MobileRedirectSchemesTests
{
    [Theory]
    [InlineData(InteractiveClient.CustomerMobile, "concertable-customer://")]
    [InlineData(InteractiveClient.VenueMobile, "concertable-business://")]
    [InlineData(InteractiveClient.ArtistMobile, "concertable-business://")]
    [InlineData(InteractiveClient.BusinessMobile, "concertable-business://")]
    public void MobileRedirectScheme_AMobileClient_IsItsNativeScheme(InteractiveClient client, string expected)
    {
        Assert.Equal(expected, client.MobileRedirectScheme);
    }

    [Theory]
    [InlineData(InteractiveClient.CustomerBrowser)]
    [InlineData(InteractiveClient.VenueBrowser)]
    [InlineData(InteractiveClient.ArtistBrowser)]
    [InlineData(InteractiveClient.BusinessBrowser)]
    [InlineData(InteractiveClient.Admin)]
    [InlineData(InteractiveClient.E2ETest)]
    public void MobileRedirectScheme_ANonMobileClient_IsNull(InteractiveClient client)
    {
        Assert.Null(client.MobileRedirectScheme);
    }
}
