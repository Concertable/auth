# Review — Refactor/PostgresAuthReplacement

- Review status: complete
- Judgment: approved
- Candidate: `47cfb32236acecae1df14ac06204361d020f7162..c71d196f202107c4450f2e3d560dba5a412b66cd` (45 paths)
- Reviewed up to commit: `94ecae62af1e7aa678b99aae4fddff210b0e6d27`
- Layers: host native/general review at `high`; parent synthesis against the routed standards
  (`always-on-instructions`, `docs-and-debt`, `migrations`, `packages`, `multitenancy`,
  `integration-testing`, `unit-testing`, `csharp-style`, `csharp-naming`)

## Findings

### R1 — `AddAuth` did not enforce the migrations dependency (major, fixed)

`AuthDbInitializer` no longer migrates, so the migrations job is the only thing that creates schema. Both
`AddAuth` overloads left the ordering to the caller, and only `local/AppHost/AppHost.cs` chained
`.WaitForCompletion(migrations)`. A composition that called `AddAuth` without repeating that — the System
companion, or any consumer of the published `Concertable.Auth.Hosting` package — would boot the web host
against a database with no schemas.

This is the shape Payment ships, and it is exactly what failed System qualification twice on the Payment
slice. Fixed structurally rather than copied: both overloads now take the migrations resource and wire
`WaitForCompletion` themselves, so the dependency cannot be forgotten. `ResourceGraphTests` asserts the
wait and its exit code.

### R2 — Npgsql provider wiring duplicated across four sites (minor, fixed)

`AuthHostExtensions`, `AuthDbContextFactory`, `PersistedGrantDbContextFactory` and `AuthMigrationJob` each
spelled out the provider, migrations assembly and history table. A history-table or schema change applied to
some sites and not others would let the job and the host disagree on where EF looks for applied migrations.
Fixed: `AuthDbContextOptionsExtensions` is the single owner, exposing `UseNpgsqlForAuth`,
`UseNpgsqlForGrants` and `UseNpgsqlForOutbox`; all four sites call it.

### R3 — post-`MigrateAsync` pending check is unreachable (trivial, accepted)

`GetPendingMigrationsAsync().Any()` after a successful `MigrateAsync` cannot be true. Kept deliberately:
it is `PaymentMigrationJob`'s shape verbatim, costs one query per context per deploy, and diverging in one
service alone would create drift for no behavioural gain. If it is wrong it is wrong in both services and
belongs to a sweep across them.

## Local verification at the reviewed head

- Release build: 0 warnings, 0 errors.
- Tests: contracts 34/34, unit 23/23, startup 13/13, integration 56/56.
- Migration drift: `./initial-migrations.ps1 -Check` clean for both contexts.
- AppHost manifest: `AuthDb` present, `B2BDb` absent, `postgres` container, `auth-migrations` project resource.
- Package readiness: `Concertable.Auth.Contracts` and `Concertable.Auth.Hosting` pack and restore into a
  clean consumer, 0 warnings.
- Images: runtime and `auth-migrations` both build from digest-pinned bases, non-root, correct entrypoints,
  no package credential in history; the migrations image refuses to run without `ConnectionStrings__AuthDb`.
- Live local composition: PostgreSQL 17.6, `AuthDb` created, schemas `auth`/`idsrv`/`messaging` present with
  their three distinct histories, migrations job exited before Auth started, 75 credentials seeded, 75
  outbox rows written, OIDC discovery returned 200.

## Delta after the first pass

`b3ffe8d` — CI run `35473167888` failed the image job at `72af247` while both images built and verified and
the script printed its success line. Cause was in this slice's own new code: the migrations-image smoke
asserts the container refuses to start without `ConnectionStrings__AuthDb`, so it exits non-zero by design,
and PowerShell propagated that last native exit code as the step's result. The fix captures the code for the
assertion and resets `$LASTEXITCODE`.

The local run that first exercised this script checked its output but not its exit code, which is why it was
not caught before the push. Verified after the fix: `./scripts/verify-auth-images.ps1` builds and verifies
both images and exits 0.

`94ecae6` — `AuthDevSeeder.MigrateAsync` still called `Database.MigrateAsync`, so the web host kept a
migration path in dev and E2E after this slice moved schema ownership to the job and documented that in
AGENTS.md. Harmless in practice, because the AppHost waits for the job, but it made the documented design
untrue. Now a no-op. Verified against a virgin database migrated by the job alone: three schemas and
histories created, job exit 0, then 75 credentials and 75 outbox rows seeded and OIDC discovery 200.

## Environment note, not a finding

`auth` never launches under the local AppHost on this workstation — DCP creates the resource and produces no
process. Confirmed pre-existing by A/B: the same happens on `main` before this cut-over, where DCP likewise
produced no output files for `auth`. The Service Bus emulator health gate is the shared suspect. It does not
affect this slice's proof: the migrations job runs and exits 0 under the AppHost and creates all three
schemas, and the web host was exercised directly against that migrated database.
