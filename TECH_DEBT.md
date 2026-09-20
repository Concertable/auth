# Concertable.Auth — Technical Debt

## The migrations job drags the whole web application into its image

`Concertable.Auth.Migrations` references `Concertable.Auth` because the DbContexts, their configuration
provider and the scaffolded migrations all live in that ASP.NET Core project. The migrations image therefore
ships the Razor Pages host and every runtime dependency it carries, and runs on the `aspnet` base rather than
`runtime`.

Resolution: extract `Concertable.Auth.Data` as a class library owning the entities, configurations, contexts,
design-time factories and migrations, then have both the web project and the migrations job reference it. The
entry is settled once `Concertable.Auth.Migrations` no longer references `Concertable.Auth` and its image
builds on the `runtime` base.
