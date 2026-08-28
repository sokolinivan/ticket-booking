## Why

TicketBooking has no persistence boundary for system identities, so later authentication and authorization work has no stable domain model, schema, or concurrency guarantees to build on. This change establishes the Identity module's domain and database foundation while preserving modular-monolith isolation.

## What Changes

- Add an Identity module with `SystemUser`, `Role`, `Permission`, `SystemUserRole`, and `RolePermission` domain models.
- Add strongly typed identifiers and explicit lifecycle state for system users without physical deletion.
- Add an Identity-owned EF Core `IdentityDbContext` mapped exclusively to the `identity` SQL schema.
- Add PostgreSQL mappings, internal foreign keys, uniqueness constraints, lookup indexes, and `Version bigint` optimistic concurrency protection.
- Add the initial Identity migration for all five tables.
- Add unit and integration coverage for domain invariants, persistence, constraints, migrations, and concurrency conflicts.
- Keep authentication flows, application use cases, audit integration, and administrator bootstrap outside this change.

## Capabilities

### New Capabilities

- `identity/domain-and-persistence`: Defines the system-user identity model and its isolated, concurrency-safe EF Core persistence contract.

### Modified Capabilities

None.

## Impact

- Adds new backend Identity module projects and solution references following the existing modular-monolith structure.
- Adds EF Core persistence types and an Identity-specific migration set targeting the shared PostgreSQL database under schema `identity`.
- Adds a shared PostgreSQL resource to AppHost and equivalent root Docker Compose configuration, including a persistent volume, health check, and connection-string setting without source-controlled secrets.
- Extends unit, integration, and architecture tests to enforce Identity invariants and module ownership.
- Adds no Identity HTTP endpoints; API changes are limited to dependency-injection composition and database configuration.
