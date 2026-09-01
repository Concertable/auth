# Concertable.Auth

The **Auth** service of [Concertable](https://github.com/Concertable/concertable) — the
authentication *adapter service*: it owns credentials and identity, issues tokens, and publishes
`CredentialRegisteredEvent` when a user registers (which every consuming service reacts to to
provision its own user/profile rows). As an adapter, data services may call it synchronously and
`WaitFor` it at startup.

## Auth promotion source

Auth promotion changes now land in this repository. During checkpoint 10B, the preserved Auth source in
[`Concertable/concertable`](https://github.com/Concertable/concertable) remains available for compatibility
and rollback, but mirror automation must not overwrite this repository. Open Auth preparation pull requests
here; canonical releases and deployment remain separately approved cutover steps.

## Building standalone

The deployable closure consumes Concertable's shared platform as NuGet `PackageReference`s from the
private org feed `https://nuget.pkg.github.com/Concertable`. Restoring them needs a GitHub
[personal access token](https://github.com/settings/tokens) with the **`read:packages`** scope,
exported as `GITHUB_PACKAGES_TOKEN` (the `nuget.config` reads it):

```sh
export GITHUB_PACKAGES_TOKEN=<your read:packages PAT>
dotnet build Concertable.Auth.slnx
```

(In this repository's CI the same variable is supplied by the workflow's short-lived `GITHUB_TOKEN`;
standalone, you export your own PAT.)

## Verifying package readiness

The repository owns two NuGet packages: `Concertable.Auth.Contracts` and `Concertable.Auth.Hosting`.
Verification packs both without publishing, checks their manifests and common version, then restores and
builds a clean temporary consumer:

```sh
pwsh ./scripts/verify-auth-packages.ps1 -Configuration Release
```

## Verifying image readiness

Auth owns a runtime image and a one-shot operational-store migration image. The verifier builds both from
digest-pinned .NET 10 bases without publishing them, exposes the package credential only as a BuildKit
restore secret, validates their non-root metadata and entrypoints, and smoke-runs the migration help path:

```sh
pwsh ./scripts/verify-auth-images.ps1
```

CI additionally performs source/image secret scans, rejects critical vulnerabilities, and validates a
CycloneDX SBOM for each image. Publishing, signing, and release tags remain separate cutover actions.
