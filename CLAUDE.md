# Concertable.Auth

This is the authorization server for the Concertable platform, built with **Duende IdentityServer** on .NET 10.

## What this project is

An OAuth 2.0 / OpenID Connect (OIDC) authorization server — a dedicated microservice responsible for:
- Authenticating users (login, registration, password reset)
- Issuing access tokens and identity tokens
- Managing refresh tokens
- Exposing OIDC discovery endpoints

It replaces the custom JWT auth that previously lived in `Concertable.Web`.

## Why Duende IdentityServer

- Industry standard for .NET authorization servers
- Implements OAuth 2.0 (RFC 6749) and OIDC Core spec fully
- Free for non-commercial/personal use
- Well documented, widely recognised in .NET job specs
- Chosen over OpenIddict (less CV recognition), Auth0/Entra (black box SaaS, no protocol visibility)

## How to behave in this project

The developer is **learning OAuth 2.0 and OIDC from the ground up**. They understand JWT basics but are new to the full protocol stack.

- **Explain everything** — when you write or change code, explain what it does and why in plain terms
- **Use protocol language** — use correct terms (authorization server, resource server, client, grant type, scope, claim, introspection) and explain them when first introduced
- **Interview mindset** — frame explanations so they prepare the developer to answer "what is X and why does it exist?" in an interview
- **Don't hide complexity** — if something is non-obvious, explain the concept before the code
- **Teach the why** — not just what the code does, but what problem in the OAuth/OIDC spec it solves

## The broader architecture

```
[React SPA]  →  [Concertable.Auth]  →  issues tokens
[React SPA]  →  [Concertable.Web]   →  validates tokens against Auth
```

- `Concertable.Auth` = authorization server (this project)
- `Concertable.Web` = resource server (existing API, validates tokens)
- React app = client (uses PKCE authorization code flow)

## User roles

The platform has four roles that map to OIDC claims:
- `Customer`
- `ArtistManager`
- `VenueManager`
- `Admin`

## Key OAuth 2.0 / OIDC concepts to understand

- **Authorization Code + PKCE** — the flow the React app uses to get tokens
- **Access token** — short-lived, proves the client can call the API
- **Identity token (id_token)** — proves who the user is (OIDC layer on top of OAuth)
- **Refresh token** — long-lived, used to get new access tokens without re-login
- **Scope** — what the client is asking permission to access
- **Claim** — a key/value pair inside a token (e.g. `role: Customer`)
- **Discovery document** — `/.well-known/openid-configuration` — tells clients where all the endpoints are
- **PKCE** — Proof Key for Code Exchange — prevents auth code interception attacks in public clients (SPAs, mobile)
