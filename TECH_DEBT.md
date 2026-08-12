# Concertable.Auth — Technical Debt

---

## LOW

### Auth integration tests run the application under the E2E environment

`Concertable.Auth.IntegrationTests.Fixtures/ApiFixture.cs` sets both host environment variables and
`WebApplicationFactory` to `E2E`. This makes the whole integration suite load E2E configuration and
registrations merely because the password-grant tests use the E2E-only `concertable-test` client and
`ResourceOwnerPasswordValidator`. It blurs the integration/E2E boundary and differs from the B2B,
Customer, and Search integration fixtures, which run under `Testing`.

**Resolves when:** the normal Auth integration fixture runs under `Testing`, and the token-flow tests
that require the E2E-only client and validator use a separate, explicitly scoped fixture or test host.

### E2E client identity and scopes are duplicated as contextual magic strings

`Concertable.Testing.E2E/TestTokenMinter.cs` posts the literal client id `concertable-test` and the literal
scope set `concertable.b2b.api concertable.customer.api concertable.search.api`, while Auth independently
registers the matching test client and API resources. The harness and authority can therefore drift with
no compile-time signal: renaming a client or scope in Auth leaves the test helper compiling but unable to
mint tokens. The shared harness should not solve this by depending on Auth runtime internals.

**Resolves when:** one Auth-owned contract/configuration source defines the client ids and scope names,
and both Auth registration and external consumers such as `TestTokenMinter` reuse it.
