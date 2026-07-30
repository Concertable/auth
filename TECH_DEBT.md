# Concertable.Auth — Technical Debt

---

## LOW

### E2E client identity and scopes are duplicated as contextual magic strings

`Concertable.E2ETests/TestTokenMinter.cs` posts the literal client id `concertable-test` and the literal
scope set `concertable.b2b.api concertable.customer.api concertable.search.api`, while Auth independently
registers the matching test client and API resources. The harness and authority can therefore drift with
no compile-time signal: renaming a client or scope in Auth leaves the test helper compiling but unable to
mint tokens. The shared harness should not solve this by depending on Auth runtime internals.

**Resolves when:** one Auth-owned contract/configuration source defines the client ids and scope names,
and both Auth registration and external consumers such as `TestTokenMinter` reuse it.
