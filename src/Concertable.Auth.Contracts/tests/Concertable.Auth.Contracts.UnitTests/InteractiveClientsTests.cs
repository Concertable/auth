namespace Concertable.Auth.Contracts.UnitTests;

public sealed class InteractiveClientsTests
{
    [Fact]
    public void All_CoversEveryEnumMemberExactlyOnce()
    {
        var members = Enum.GetValues<InteractiveClient>();

        Assert.Equal(members.Length, InteractiveClients.All.Count);
        Assert.Equal(members.ToHashSet(), InteractiveClients.All.Select(info => info.Client).ToHashSet());
    }

    [Fact]
    public void Info_EveryMember_ReturnsItsOwnRow()
    {
        foreach (var client in Enum.GetValues<InteractiveClient>())
            Assert.Equal(client, client.Info().Client);
    }

    [Fact]
    public void All_WireIdsAreDistinct()
    {
        var ids = InteractiveClients.All.Select(info => info.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData("customer-web", InteractiveClient.CustomerBrowser)]
    [InlineData("customer-mobile", InteractiveClient.CustomerMobile)]
    [InlineData("venue-web", InteractiveClient.VenueBrowser)]
    [InlineData("venue-mobile", InteractiveClient.VenueMobile)]
    [InlineData("artist-web", InteractiveClient.ArtistBrowser)]
    [InlineData("artist-mobile", InteractiveClient.ArtistMobile)]
    [InlineData("admin", InteractiveClient.Admin)]
    [InlineData("concertable-test", InteractiveClient.E2ETest)]
    public void Find_AKnownWireId_ResolvesItsRow(string clientId, InteractiveClient expected)
    {
        var info = InteractiveClients.Find(clientId);

        Assert.NotNull(info);
        Assert.Equal(expected, info.Value.Client);
        Assert.Equal(clientId, info.Value.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nope")]
    [InlineData("Customer-Web")]
    [InlineData("customer_web")]
    public void Find_AnAbsentOrUnknownWireId_ReturnsNull(string? clientId)
    {
        Assert.Null(InteractiveClients.Find(clientId));
    }

    [Fact]
    public void IsMobile_TracksMobileSchemePresence()
    {
        foreach (var info in InteractiveClients.All)
            Assert.Equal(info.MobileScheme is not null, info.IsMobile);
    }

    [Theory]
    [InlineData(InteractiveClient.CustomerMobile, "concertable-customer://")]
    [InlineData(InteractiveClient.VenueMobile, "concertable-business://")]
    [InlineData(InteractiveClient.ArtistMobile, "concertable-business://")]
    public void MobileScheme_IsTheNativeRedirectScheme(InteractiveClient client, string expected)
    {
        Assert.Equal(expected, client.Info().MobileScheme);
    }

    [Fact]
    public void BrowserAndPartyAgnosticClients_HaveNoMobileScheme()
    {
        Assert.Null(InteractiveClient.CustomerBrowser.Info().MobileScheme);
        Assert.Null(InteractiveClient.Admin.Info().MobileScheme);
        Assert.Null(InteractiveClient.E2ETest.Info().MobileScheme);
    }
}
