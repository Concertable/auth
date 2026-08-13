using Concertable.Auth.IntegrationTests.Fixtures;

[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<ApiFixture>;

[CollectionDefinition("E2EToken")]
public sealed class E2ETokenCollection : ICollectionFixture<E2ETokenApiFixture>;
