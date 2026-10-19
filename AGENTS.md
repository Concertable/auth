# Concertable.Auth

Identity-only adapter (OIDC issuer). Responsibilities/flows → [`ARCHITECTURE.md`](./ARCHITECTURE.md).

## UI is server-rendered Razor Pages, not controllers/SPA

Sign-in/up/verify/reset are Razor `PageModel`s under `Pages/Account/`; the api/ controller/DTO/Response conventions don't govern them.

## Duende config is in code

Clients, scopes, resources live in `Config.cs` + `Program.cs` (in-memory) — add one there. The identity-only-B2B vs `role`+`owner`-Customer claim split is enforced in `Config.ApiResources`. Every client id and scope name is a `Concertable.Auth.Contracts.InteractiveClient` / `AuthScope` / `AuthResource` — `Config.cs` and the external harness `TestTokenMinter` both bind them, so the E2E `concertable-test` client cannot drift from what the harness requests. Add a new client to `InteractiveClient` + `InteractiveClients`, never as a bare string; Contracts is a `ProjectReference` sibling here, so only out-of-repo consumers wait on a publish.

## Three PostgreSQL migration contexts, applied by a job and not by the web host

Auth owns `AuthDbContext` (`auth` schema) and Duende's `PersistedGrantDbContext` (`idsrv` schema), both re-scaffolded by `initial-migrations.ps1`; the shared `OutboxDbContext` (`messaging`) ships its migrations in `Concertable.Messaging.Infrastructure`. All three run against `AuthDb` on PostgreSQL and each keeps its history in its own schema. `Concertable.Auth.Migrations` applies them as a run-to-completion job that Web waits for, so no replica creates schema.

<!-- BEGIN personal:current-solution sha256:2e091b5f18f67fb2302024e817c71d4a0f5234c1ca63ca1c9af5c4eef3b6a128 -->
# Personal projects

These rules are mandatory in projects that explicitly adopt `personal@tj-agents`. Adoption records the
project's personal scope; a home directory, author name or employer association never establishes it.
Employer projects remain outside this profile unless Tommy explicitly changes that scope.

Keep one current supported solution. When replacing behavior, remove the superseded names, aliases,
fallback implementations, adapters, configuration formats and compatibility branches. Coordinate and
migrate all known callers, project declarations, installed registrations, generated artifacts and live
runtime entry points in the same cutover. Do not retain obsolete behavior for hypothetical external users;
coordinate upgrades with the actual collaborating developers.

Verify the entry points people actually use on every supported platform, including Windows and Linux
launch paths where supported. Source edits, successful generation, passing unit tests and version commands
alone do not prove that a migration is complete. Check installed bindings and fresh runtime behavior.
Record any unverified entry point as incomplete, with its resolver and observable acceptance condition.

For example, replacing `old-launch` with `launch` means migrating the registered shell command, project
configuration and actual callers, deleting the old entry, then invoking `launch` from a fresh session on
each supported OS. Running `launch --version` alone leaves the cutover unverified.

```text
Replace old-launch with launch:
1. Migrate the registered shell command, configuration and actual callers.
2. Delete the old entry and regenerate installed settings.
3. Invoke launch from a fresh supported Windows/Linux session and check its actual behavior.
Result: launch --version alone leaves this migration incomplete.
```

Do not independently add a backwards-compatibility exception. Surface an explicit contrary user requirement
as a deliberate policy decision, identify the affected supported behavior and reconcile this profile's
scope before implementing it. Never hide an exception in a fallback or adapter.

Solve the demonstrated problem with the smallest suitable implementation. Add abstractions, dependencies,
configuration switches, duplicate paths or process machinery only for a concrete current need. Prefer the
capabilities the project already selects. Verify the behavior and meaningful failure paths in proportion
to the change; do not add tests that merely repeat the implementation or a checklist with no concrete benefit.

This policy is authored once here. Personal adoption generates its marked native instruction block from
this body and checks it for drift. A selected plugin or an `always` tier alone is not proof of loading.
<!-- END personal:current-solution -->
