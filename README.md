# Concertable.Auth

The **Auth** service of [Concertable](https://github.com/Concertable/concertable) — the
authentication *adapter service*: it owns credentials and identity, issues tokens, and publishes
`CredentialRegisteredEvent` when a user registers (which every consuming service reacts to to
provision its own user/profile rows). As an adapter, data services may call it synchronously and
`WaitFor` it at startup.

## Canonical source vs. this mirror

Development happens in the **monorepo** ([`Concertable/concertable`](https://github.com/Concertable/concertable)),
under `api/Concertable.Auth/`. That folder is **automatically mirrored** to the read-only repo
[`Concertable/auth`](https://github.com/Concertable/auth) on every push to
`master`. **Don't open PRs against the mirror** — nothing flows back from it.

## Building standalone

The deployable closure consumes Concertable's shared platform as NuGet `PackageReference`s from the
private org feed `https://nuget.pkg.github.com/Concertable`. Restoring them needs a GitHub
[personal access token](https://github.com/settings/tokens) with the **`read:packages`** scope,
exported as `GITHUB_PACKAGES_TOKEN` (the `nuget.config` reads it):

```sh
export GITHUB_PACKAGES_TOKEN=<your read:packages PAT>
dotnet build src/Concertable.Auth/Concertable.Auth.csproj
```

(In the monorepo's CI the same variable is supplied by the workflow's `GITHUB_TOKEN`; standalone,
you export your own PAT.)
