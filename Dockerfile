# syntax=docker/dockerfile:1.28.0@sha256:bb22d9815c728170f72750f4e5b0d672e06176142e1d602c7e66c050100b7e5b

ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0@sha256:747cbade5bfd918a175ac32f8f1d931edec2c7d0d5397a3de72eaef1ac1f66d2
ARG DOTNET_ASPNET_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0.13@sha256:8af87eba5e7cdafb1514d7388be287365da97c132509e84dfe252ffd552398ca
ARG BUILD_VERSION=0.0.0-local

FROM ${DOTNET_SDK_IMAGE} AS auth-restore
WORKDIR /source
COPY Directory.Build.props Directory.Build.targets Directory.Packages.props nuget.config README.md ./
COPY api/src/Concertable.Auth.Contracts/Concertable.Auth.Contracts.csproj api/src/Concertable.Auth.Contracts/
COPY api/src/Concertable.Auth.Contracts/Directory.Build.props api/src/Concertable.Auth.Contracts/
COPY api/src/Concertable.Auth.Contracts/Directory.Packages.props api/src/Concertable.Auth.Contracts/
COPY api/src/Concertable.Auth/Concertable.Auth.csproj api/src/Concertable.Auth/
RUN --mount=type=secret,id=github_packages_token \
    test -s /run/secrets/github_packages_token && \
    GITHUB_PACKAGES_TOKEN="$(cat /run/secrets/github_packages_token)" \
    dotnet restore api/src/Concertable.Auth/Concertable.Auth.csproj

FROM auth-restore AS auth-publish
ARG BUILD_VERSION
COPY api/src/Concertable.Auth.Contracts/ api/src/Concertable.Auth.Contracts/
COPY api/src/Concertable.Auth/ api/src/Concertable.Auth/
RUN dotnet publish api/src/Concertable.Auth/Concertable.Auth.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false \
    /p:MinVerVersionOverride="${BUILD_VERSION}"

FROM ${DOTNET_ASPNET_IMAGE} AS auth-runtime
ARG VCS_REF
ARG BUILD_VERSION
LABEL org.opencontainers.image.source="https://github.com/Concertable/auth" \
      org.opencontainers.image.revision="${VCS_REF}" \
      org.opencontainers.image.version="${BUILD_VERSION}" \
      org.opencontainers.image.title="Concertable Auth" \
      org.opencontainers.image.description="Concertable credential and OIDC service"
WORKDIR /app
COPY --from=auth-publish --chown=$APP_UID:$APP_UID /app/publish/ ./
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Concertable.Auth.dll"]

FROM auth-restore AS migration-publish
ARG BUILD_VERSION
COPY api/src/Concertable.Auth.Migrations/Concertable.Auth.Migrations.csproj api/src/Concertable.Auth.Migrations/
RUN --mount=type=secret,id=github_packages_token \
    test -s /run/secrets/github_packages_token && \
    GITHUB_PACKAGES_TOKEN="$(cat /run/secrets/github_packages_token)" \
    dotnet restore api/src/Concertable.Auth.Migrations/Concertable.Auth.Migrations.csproj
COPY api/src/Concertable.Auth.Contracts/ api/src/Concertable.Auth.Contracts/
COPY api/src/Concertable.Auth/ api/src/Concertable.Auth/
COPY api/src/Concertable.Auth.Migrations/ api/src/Concertable.Auth.Migrations/
RUN dotnet publish api/src/Concertable.Auth.Migrations/Concertable.Auth.Migrations.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false \
    /p:MinVerVersionOverride="${BUILD_VERSION}"

FROM ${DOTNET_ASPNET_IMAGE} AS auth-migrations
ARG VCS_REF
ARG BUILD_VERSION
LABEL org.opencontainers.image.source="https://github.com/Concertable/auth" \
      org.opencontainers.image.revision="${VCS_REF}" \
      org.opencontainers.image.version="${BUILD_VERSION}" \
      org.opencontainers.image.title="Concertable Auth migrations" \
      org.opencontainers.image.description="Applies the Auth, Duende operational-store and outbox schemas"
WORKDIR /app
COPY --from=migration-publish --chown=$APP_UID:$APP_UID /app/publish/ ./
ENV DOTNET_EnableDiagnostics=0
USER $APP_UID
ENTRYPOINT ["dotnet", "Concertable.Auth.Migrations.dll"]
