# Concertable.Auth — Technical Debt

---

## LOW

### `AuthDevSeeder` seeds no credential for `SeedState.UnverifiedVenueManager`

`Data/Seeders/AuthDevSeeder.cs` seeds OIDC credentials for `SeedUsers.Admin`, the customers, and
`SeedUsers.Managers` only. B2B's `SeedState.UnverifiedVenueManager` (`tenant-verification-gate@test.com`)
is deliberately outside `SeedUsers.Managers` (so it touches no shared cross-service seed package), so it
has **no way to log in** — the tenant-verification submit → admin-review flow can't be manually smoked in
dev without first poking the `tenant.Verifications` row in the DB.

**Resolves when:** `AuthDevSeeder` also seeds a `VenueWeb`-client credential for that user id / email, so a
fresh dev stack has one venue manager in the unverified state.

### E2E client identity and scopes are duplicated as contextual magic strings

`Concertable.Testing.E2E/TestTokenMinter.cs` posts the literal client id `concertable-test` and the literal
scope set `concertable.b2b.api concertable.customer.api concertable.search.api`, while Auth independently
registers the matching test client and API resources. The harness and authority can therefore drift with
no compile-time signal: renaming a client or scope in Auth leaves the test helper compiling but unable to
mint tokens. The shared harness should not solve this by depending on Auth runtime internals.

**Resolves when:** one Auth-owned contract/configuration source defines the client ids and scope names,
and both Auth registration and external consumers such as `TestTokenMinter` reuse it.
