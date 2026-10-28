# Concertable.Auth

Identity-only adapter (OIDC issuer). Responsibilities/flows → [`ARCHITECTURE.md`](./ARCHITECTURE.md).

## UI is server-rendered Razor Pages, not controllers/SPA

Sign-in/up/verify/reset are Razor `PageModel`s under `Pages/Account/`; the api/ controller/DTO/Response conventions don't govern them.

## Duende config is in code

Clients, scopes, resources live in `Config.cs` + `Program.cs` (in-memory) — add one there. The identity-only-B2B vs `role`+`owner`-Customer claim split is enforced in `Config.ApiResources`. Every client id and scope name is a `Concertable.Auth.Contracts.InteractiveClient` / `AuthScope` / `AuthResource` — `Config.cs` and the external harness `TestTokenMinter` both bind them, so the E2E `concertable-test` client cannot drift from what the harness requests. Add a new client to `InteractiveClient` + `InteractiveClients`, never as a bare string; Contracts is a `ProjectReference` sibling here, so only out-of-repo consumers wait on a publish.

## Three PostgreSQL migration contexts, applied by a job and not by the web host

Auth owns `AuthDbContext` (`auth` schema) and Duende's `PersistedGrantDbContext` (`idsrv` schema), both re-scaffolded by `initial-migrations.ps1`; the shared `OutboxDbContext` (`messaging`) ships its migrations in `Concertable.Messaging.Infrastructure`. All three run against `AuthDb` on PostgreSQL and each keeps its history in its own schema. `Concertable.Auth.Migrations` applies them as a run-to-completion job that Web waits for, so no replica creates schema.

<!-- BEGIN personal:rules sha256:eee7a719bb90e67aaa81ac101a45836dd099bc745fee61216c99ec7d6075f12d -->
# Personal rules

These rules govern every project that explicitly adopts `personal@tj-agents`. Adoption records the
project's personal scope; a home directory, author name or employer association never establishes it, and
employer projects stay outside this profile unless the user explicitly extends that scope. Before planning
or starting work, confirm the task is clear against each rule below.

## 1. Clean cut

Keep one current supported solution. When replacing behavior, remove the superseded names, aliases,
fallback implementations, adapters, configuration formats, compatibility branches and forwarding or
migration docs. Migrate all known callers, project declarations, installed registrations, generated
artifacts and live runtime entry points in the same cutover, and coordinate upgrades with the actual
collaborating developers. Run each migration once during the cutover and delete it; never ship migration
or cleanup code that runs again later. CI fails a commit that removes or renames a skill while a tracked source
still names its old `<package>:<name>`.

Verify the entry points people actually use on every supported platform, including Windows and Linux
launch paths where supported. Source edits, successful generation, passing unit tests and version commands
alone leave a migration unproven; check installed bindings and fresh runtime behavior. Record any
unverified entry point as incomplete, with its resolver and observable acceptance condition.

```text
Replace old-launch with launch:
1. Migrate the registered shell command, configuration and actual callers.
2. Delete the old entry and regenerate installed settings.
3. Invoke launch from a fresh supported Windows/Linux session and check its actual behavior.
Result: launch --version alone leaves this migration incomplete.
```

A backwards-compatibility exception exists only as an explicit user requirement. Surface it as a
deliberate policy decision, identify the affected supported behavior and reconcile this profile's scope
before implementing it, keeping the exception visible rather than inside a fallback or adapter.

## 2. Proportionate implementation

Solve the demonstrated problem with the smallest suitable implementation. Add abstractions, dependencies,
configuration switches, duplicate paths or process machinery only for a concrete current need. Prefer the
capabilities the project already selects. Verify the behavior and meaningful failure paths in proportion
to the change, with tests that prove behavior rather than restate the implementation.

## 3. Plans

Track work as a GitHub issue or project item. A plan committed under `plans/` is only the detailed
implementation plan behind one such item and links to it; working plans stay machine-local.

This policy is authored once here. A selecting project receives it at SessionStart, and adoption generates
its marked native instruction block from this body and checks it for drift. A selected plugin or an
`always` tier alone is not proof of loading.
<!-- END personal:rules -->
