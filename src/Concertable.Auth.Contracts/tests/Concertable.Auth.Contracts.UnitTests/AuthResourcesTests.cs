namespace Concertable.Auth.Contracts.UnitTests;

public sealed class AuthResourcesTests
{
    [Fact]
    public void All_CoversEveryEnumMemberExactlyOnce()
    {
        var members = Enum.GetValues<AuthResource>();

        Assert.Equal(members.Length, AuthResources.All.Count);
        Assert.Equal(members.ToHashSet(), AuthResources.All.ToHashSet());
    }

    [Theory]
    [InlineData(AuthResource.B2B, "concertable.b2b.api")]
    [InlineData(AuthResource.Customer, "concertable.customer.api")]
    [InlineData(AuthResource.Search, "concertable.search.api")]
    [InlineData(AuthResource.Payment, "concertable.payment.api")]
    public void Audience_IsTheResourceServerAudience(AuthResource resource, string expected)
    {
        Assert.Equal(expected, resource.Audience);
    }

    [Fact]
    public void Payment_AudienceDiffersFromItsScope()
    {
        Assert.Equal("concertable.payment.api", AuthResource.Payment.Audience);
        Assert.Equal("payment:write", AuthScope.PaymentWrite.Id);
    }

    [Fact]
    public void AcceptedScopesAndIncludedClaims_MatchTheDuendeRegistration()
    {
        Assert.Equal([AuthScope.B2BApi], AuthResource.B2B.AcceptedScopes.ToArray());
        Assert.Equal(["email"], AuthResource.B2B.IncludedClaims.ToArray());

        Assert.Equal([AuthScope.CustomerApi, AuthScope.UserClaims], AuthResource.Customer.AcceptedScopes.ToArray());
        Assert.Equal(["role", "owner"], AuthResource.Customer.IncludedClaims.ToArray());

        Assert.Equal([AuthScope.SearchApi], AuthResource.Search.AcceptedScopes.ToArray());
        Assert.Empty(AuthResource.Search.IncludedClaims);

        Assert.Equal([AuthScope.PaymentWrite], AuthResource.Payment.AcceptedScopes.ToArray());
        Assert.Empty(AuthResource.Payment.IncludedClaims);
    }

    [Fact]
    public void AudiencesAreDistinct()
    {
        var audiences = AuthResources.All.Select(resource => resource.Audience).ToList();

        Assert.Equal(audiences.Count, audiences.Distinct(StringComparer.Ordinal).Count());
    }
}
