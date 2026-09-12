# Concertable.Auth.Contracts.UnitTests — unit tests

Covers the published `Concertable.Auth.Contracts` typed identity model — the `InteractiveClients`,
`AuthScopes` and `AuthResources` frozen catalogs: completeness, wire-id round-trips and `TryGet` misses.
The project under test is the parent `src/Concertable.Auth.Contracts/` package.

**Unit-only: a test that needs a host, HTTP, a container or a database belongs elsewhere.**

Conventions: the `dotnet-standards:unit-testing` skill, plus `dotnet:unit-testing` for this system's
test-tier gate.
