# Concertable.Auth

Identity-only adapter (OIDC issuer). Responsibilities/flows → [`ARCHITECTURE.md`](./ARCHITECTURE.md).

## UI is server-rendered Razor Pages, not controllers/SPA

Sign-in/up/verify/reset are Razor `PageModel`s under `Pages/Account/`; the api/ controller/DTO/Response conventions don't govern them.

## Duende config is in code

Clients, scopes, resources live in `Config.cs` + `Program.cs` (in-memory) — add one there. The identity-only-B2B vs `role`+`owner`-Customer claim split is enforced in `Config.ApiResources`. Every client id and scope name is a `Concertable.Auth.Contracts.InteractiveClient` / `AuthScope` / `AuthResource` — `Config.cs` and the external harness `TestTokenMinter` both bind them, so the E2E `concertable-test` client cannot drift from what the harness requests. Add a new client to `InteractiveClient` + `InteractiveClients`, never as a bare string; Contracts is a `ProjectReference` sibling here, so only out-of-repo consumers wait on a publish.

## Two migration contexts; the grant store lives in B2BDb

Auth owns `AuthDbContext` (Auth schema) **and** Duende's `PersistedGrantDbContext` (`idsrv` schema), both re-scaffolded by `initial-migrations.ps1`. The operational/persisted-grant store runs against **`B2BDb`**, not `AuthDb`, so the AppHost provisions both databases.
