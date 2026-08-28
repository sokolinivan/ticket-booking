# Brainstorm Summary

- Change: identity-domain-and-persistence
- Date: 2026-08-27

## Confirmed Technical Approach

- Use PostgreSQL and the repository's existing Npgsql/Aspire PostgreSQL stack rather than introducing SQL Server.
- Use an explicit `Version bigint` optimistic concurrency token rather than PostgreSQL `xmin`.
- Implement one `TicketBooking.Identity.Core` project containing the domain model and internal EF Core persistence. Expose only a module-registration entry point; defer a Contracts project until a later change introduces real inter-module contracts.
- Add a shared PostgreSQL resource to AppHost and equivalent root Docker Compose service, volume, health check, and API connection-string configuration.
- Translate recognized PostgreSQL uniqueness violations and EF concurrency failures into controlled Identity errors; preserve unknown database failures.

## Key Trade-offs and Risks

- The source task mentioned SQL Server and rowversion, but authoritative repository package configuration is PostgreSQL-based. The OpenSpec proposal and high-level design must be corrected before implementation.
- Adding persistence requires a PostgreSQL AppHost resource; the root Docker Compose configuration must be created or updated to remain equivalent.
- Application-managed versioning requires a reliable increment strategy for every tracked update.
- Keeping domain and persistence in one assembly is intentionally minimal, but internal namespaces and architecture tests must prevent persistence types from becoming module API.

## Testing Strategy

- Use real PostgreSQL integration tests for migrations, uniqueness, relationships, and stale-write behavior.
- Keep domain invariant tests independent from EF Core.
- Add model metadata tests for schema, converters, keys, indexes, and concurrency configuration.
- Add architecture tests that prevent dependencies on internal Identity persistence.
- Add a composition test that builds the API dependency-injection container and creates `IdentityDbContext` with the configured PostgreSQL connection string; this change adds no Identity HTTP endpoints.

## Spec Patches

- Applied: clarified that stale writes using `Version bigint` are rejected and do not overwrite committed changes.
- Applied: added controlled handling for PostgreSQL uniqueness conflicts and preservation of unknown failures.
- Applied: required AppHost and root Docker Compose parity for the shared PostgreSQL database and connection-string setting.
- Applied: replaced SQL Server/rowversion assumptions with PostgreSQL/Npgsql in supporting OpenSpec artifacts.
