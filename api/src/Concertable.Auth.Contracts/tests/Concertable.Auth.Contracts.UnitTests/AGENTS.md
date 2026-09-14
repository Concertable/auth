# Concertable.Auth.Contracts.UnitTests — unit tests

Covers the published `Concertable.Auth.Contracts` typed identity model — `InteractiveClientInfo`,
`AuthScopes` and `AuthResources`, each a frozen catalog owning its own lookup: completeness, wire-id
round-trips, `GetOrDefault` misses. The project under test is the parent `api/src/Concertable.Auth.Contracts/`
package, which grants this project `InternalsVisibleTo` so the roster-completeness tests can read
`InteractiveClientInfo.All`.

**Unit-only: a test that needs a host, HTTP, a container or a database belongs elsewhere.**

Conventions: the `dotnet-standards:unit-testing` skill, plus `dotnet:unit-testing` for this system's
test-tier gate.
