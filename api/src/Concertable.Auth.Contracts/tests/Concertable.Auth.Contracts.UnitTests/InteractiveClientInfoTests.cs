namespace Concertable.Auth.Contracts.UnitTests;

public sealed class InteractiveClientInfoTests
{
    [Fact]
    public void All_CoversEveryEnumMemberExactlyOnce()
    {
        var members = Enum.GetValues<InteractiveClient>();

        Assert.Equal(members.Length, InteractiveClientInfo.All.Count);
        Assert.Equal(members.ToHashSet(), InteractiveClientInfo.All.Select(info => info.Client).ToHashSet());
    }

    [Fact]
    public void Get_EveryMember_ReturnsItsOwnRow()
    {
        foreach (var client in Enum.GetValues<InteractiveClient>())
            Assert.Equal(client, InteractiveClientInfo.Get(client).Client);
    }

    [Fact]
    public void All_WireIdsAreDistinct()
    {
        var ids = InteractiveClientInfo.All.Select(info => info.Id).ToList();

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
    public void GetOrDefault_AKnownWireId_ResolvesItsRow(string clientId, InteractiveClient expected)
    {
        var info = InteractiveClientInfo.GetOrDefault(clientId);

        Assert.NotNull(info);
        Assert.Equal(expected, info.Client);
        Assert.Equal(clientId, info.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nope")]
    [InlineData("Customer-Web")]
    [InlineData("customer_web")]
    public void GetOrDefault_AnAbsentOrUnknownWireId_ReturnsNull(string? clientId)
    {
        Assert.Null(InteractiveClientInfo.GetOrDefault(clientId));
    }
}
