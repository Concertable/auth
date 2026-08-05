# Concertable.Auth

Identity-only adapter (OIDC issuer). Inherits root [`AGENTS.md`](../../AGENTS.md) + [`api/AGENTS.md`](../AGENTS.md); responsibilities/flows → [`ARCHITECTURE.md`](./ARCHITECTURE.md).

## UI is server-rendered Razor Pages, not controllers/SPA

Sign-in/up/verify/reset are Razor `PageModel`s under `Pages/Account/`; the api/ controller/DTO/Response conventions don't govern them.

## Duende config is in code

Clients, scopes, resources live in `Config.cs` + `Program.cs` (in-memory) — add one there. The identity-only-B2B vs `role`+`owner`-Customer claim split is enforced in `Config.ApiResources`. Keep the E2E `concertable-test` client in sync with the harness `TestTokenMinter` (→ [`TECH_DEBT.md`](./TECH_DEBT.md)).

## Two migration contexts; the grant store lives in B2BDb

Auth owns `AuthDbContext` (Auth schema) **and** Duende's `PersistedGrantDbContext` (`idsrv` schema), both re-scaffolded by `initial-migrations.ps1`. The operational/persisted-grant store runs against **`B2BDb`**, not `AuthDb`, so the AppHost provisions both databases.
