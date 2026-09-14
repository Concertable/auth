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
// api/src/Concertable.Auth.Contracts/Events/CredentialRegisteredEvent.cs
public record CredentialRegisteredEvent(Guid UserId, string Email, string ClientId) : IIntegrationEvent;
```

`ClientId` values are the `InteractiveClients` catalog in `api/src/Concertable.Auth.Contracts/InteractiveClients.cs`,
keyed by the `InteractiveClient` enum; `InteractiveClients.Find` resolves a wire id back to its row:

| ClientId | InteractiveClient | Surface |
|---|---|---|
| `customer-web` | `CustomerBrowser` | Customer web SPA |
| `customer-mobile` | `CustomerMobile` | Customer mobile app |
| `venue-web` | `VenueBrowser` | Venue Manager web SPA |
| `venue-mobile` | `VenueMobile` | Venue Manager mobile app |
| `artist-web` | `ArtistBrowser` | Artist Manager web SPA |
| `artist-mobile` | `ArtistMobile` | Artist Manager mobile app |
| `admin` | `Admin` | Admin web SPA |
| `concertable-test` | `E2ETest` | E2E harness (resource-owner password) |

## Downstream Handlers

Each service independently decides how to react to `CredentialRegisteredEvent`:

| Service | Handler | Behaviour |
|---|---|---|
| **B2B** | `CredentialRegisteredHandler` | Accepts B2B client IDs and creates the role-agnostic `UserEntity` projection; the admin client also creates `AdminProfileEntity`. Ignores non-B2B clients. |
| **Customer** | `UserCreationHandler` | Creates a role-agnostic `UserEntity`. Ignores non-customer clients. |
| **Payment** | `CustomerRegisteredHandler` | Provisions Stripe Customer account for customer clients. |
| **Payment** | `ManagerRegisteredHandler` | Provisions Stripe Customer + Connect accounts for B2B clients. |

All handlers use the **inbox pattern** for idempotency.

## Claims Enrichment

Auth's `ProfileService` delegates to `IProfileClaimsProvider` implementations:

| Provider | Claims | Source |
|---|---|---|
| `LocalProfileClaimsProvider` | `email`, `email_verified` | Auth DB |
| `RemoteProfileClaimsProvider` (Customer only) | Customer-owned `role` and `owner` | HTTP call to Customer's `/internal/users/{sub}/claims` |

Auth never stores authority claims directly. B2B tokens are identity-only; Customer's transitional `role` and `owner` claims are fetched from Customer at token issuance time.

## What Auth Does NOT Own

- User roles or kinds
- Business-domain user profiles (venue, artist, customer)
- Stripe account provisioning
- Any cross-module data
