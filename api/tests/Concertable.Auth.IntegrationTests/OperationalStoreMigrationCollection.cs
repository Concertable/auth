namespace Concertable.Auth.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class OperationalStoreMigrationCollection : ICollectionFixture<OperationalStoreMigrationFixture>
{
    public const string Name = "OperationalStoreMigration";
}
