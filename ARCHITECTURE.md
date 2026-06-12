# Concertable.Auth — Architecture

## Responsibilities

Auth is a **credential-only** service. It owns:
- Email + password hash storage (`CredentialEntity`)
- Email verification state
- Duende IdentityServer configuration (clients, scopes, signing keys)
- The sign-up and sign-in Razor Pages (`Pages/Account/`)
- Token issuance and claims enrichment via `IProfileService`

Auth has **no knowledge of roles, user kinds, or business domains**. It does not know whether a registering user is a Venue Manager, Artist Manager, or Customer.

## Sign-Up Flow

```
Browser (SPA)
  └─ initiates OAuth authorization with clientId (e.g. "venue-web")
       └─ Auth extracts clientId from the OAuth context
            └─ CredentialEntity.Create(email, passwordHash, clientId)
                 └─ raises CredentialCreatedDomainEvent(credential, clientId)
                      └─ CredentialCreatedDomainEventHandler (pre-commit)
                           └─ publishes CredentialRegisteredEvent(UserId, Email, ClientId)
```

The `clientId` is **not stored** in Auth — it is forwarded on the integration event only.

## CredentialRegisteredEvent

```csharp
// Concertable.Auth.Contracts/Events/CredentialRegisteredEvent.cs
public record CredentialRegisteredEvent(Guid UserId, string Email, string ClientId) : IIntegrationEvent;
```

`ClientId` values are defined in `Concertable.Auth.Contracts/ClientIds.cs`:

| ClientId | Surface |
|---|---|
| `customer-web` | Customer web SPA |
| `customer-mobile` | Customer mobile app |
| `venue-web` | Venue Manager web SPA |
| `venue-mobile` | Venue Manager mobile app |
| `artist-web` | Artist Manager web SPA |
| `artist-mobile` | Artist Manager mobile app |

## Downstream Handlers

Each service independently decides how to react to `CredentialRegisteredEvent`:

| Service | Handler | Behaviour |
|---|---|---|
| **B2B** | `CredentialRegisteredHandler` | Maps `ClientId` → `Role`; creates `UserEntity` + role-specific profile (`VenueManagerProfileEntity` or `ArtistManagerProfileEntity`). Ignores non-B2B clients. |
| **Customer** | `UserCreationHandler` | Creates a role-agnostic `UserEntity`. Ignores non-customer clients. |
| **Payment** | `CustomerRegisteredHandler` | Provisions Stripe Customer account for customer clients. |
| **Payment** | `ManagerRegisteredHandler` | Provisions Stripe Customer + Connect accounts for B2B clients. |

All handlers use the **inbox pattern** for idempotency.

## Claims Enrichment

Auth's `ProfileService` delegates to `IProfileClaimsProvider` implementations:

| Provider | Claims | Source |
|---|---|---|
| `LocalProfileClaimsProvider` | `email`, `email_verified` | Auth DB |
| `RemoteProfileClaimsProvider` (registered once per source: B2B, Customer) | `role`, `owner` (whatever the source issues) | HTTP call to the source service's `/internal/users/{sub}/claims` |

Auth never stores role claims directly. The `role` claim is owned by B2B and fetched at token issuance time.

## What Auth Does NOT Own

- User roles or kinds
- Business-domain user profiles (venue, artist, customer)
- Stripe account provisioning
- Any cross-module data
