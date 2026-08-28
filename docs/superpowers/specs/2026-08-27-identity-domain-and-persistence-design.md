---
comet_change: identity-domain-and-persistence
role: technical-design
canonical_spec: openspec
archived-with: 2026-08-28-identity-domain-and-persistence
status: final
---

# Identity Domain And Persistence Technical Design

## Context

TicketBooking is an early modular monolith. Its Companies module currently consists of empty Contracts and Core project shells, while the API and test projects remain close to templates. Central package management already selects EF Core 10, Npgsql, Aspire PostgreSQL, and PostgreSQL telemetry. This change therefore establishes both the first substantial module persistence boundary and the shared PostgreSQL runtime topology.

The canonical requirements are the OpenSpec delta under `docs/openspec/changes/identity-domain-and-persistence/`. This document refines implementation boundaries, data mappings, failure semantics, migration ownership, and verification.

## Architecture

Add one production project, `TicketBooking.Identity.Core`, under `src/Backend/Modules`. It contains the domain model and internal persistence implementation. A separate Contracts project is deferred because this change introduces no inter-module API; the later authorization/application change can add contracts when consumers and compatibility requirements are known.

The assembly exposes a narrow module-registration extension used by `TicketBooking.Api`. `IdentityDbContext`, EF configurations, migration types, and persistence error translation remain internal. Architecture tests prevent other modules from referencing internal Identity persistence namespaces or types.

```text
TicketBooking.Api
    |
    | AddIdentityModule(configuration)
    v
TicketBooking.Identity.Core
    |-- Domain
    |-- Internal/Persistence
    |     |-- IdentityDbContext
    |     |-- Configurations
    |     |-- Migrations
    |     `-- Persistence error translation
    `-- Module registration

AppHost / Docker Compose
    |
    `-- shared PostgreSQL database
             `-- identity schema
```

## Domain Model

`SystemUser` is the aggregate root for profile data, credentials, lifecycle, login tracking, audit metadata, role memberships, and optimistic concurrency state. Its identifier is `SystemUserId`, a readonly record struct backed by `Guid` and mapped to PostgreSQL `uuid`.

`Role` and `Permission` are independently identified Identity entities because they have stable codes and lifecycles separate from one user. `SystemUserRole` and `RolePermission` are explicit join entities. `SystemUserRole` stores assignment time and actor; both join types enforce one association per pair.

Required user statuses are `Active`, `Blocked`, `Disabled`, and `Archived`. They are stored as stable strings. Archived users remain persisted; normal lifecycle behavior never physically deletes a user.

Domain factories and mutation methods validate required values and state transitions. Database constraints repeat critical guarantees so invalid concurrent or external writes cannot bypass them.

## Persistence Model

`IdentityDbContext` uses `identity` as its default schema and owns these tables:

| Table | Key guarantees |
| --- | --- |
| `SystemUsers` | PK `Id`, unique `NormalizedLogin`, indexes on `Email` and `Status`, concurrency `Version` |
| `Roles` | PK `Id`, unique `Code` |
| `Permissions` | PK `Id`, unique `Code` |
| `SystemUserRoles` | Identity-internal FKs, unique `(SystemUserId, RoleId)`, assignment metadata |
| `RolePermissions` | Identity-internal FKs, unique `(RoleId, PermissionId)` |

Strongly typed IDs use explicit value converters and value comparers. String lengths, nullability, delete behavior, constraint names, and index names are configured explicitly. Physical foreign keys never cross module schemas.

Identity migrations live in the Identity assembly and use a migration history table in the `identity` schema. This prevents future module migrations from sharing ownership metadata even though modules use one physical PostgreSQL database.

## Optimistic Concurrency

Mutable aggregate rows use an explicit `Version bigint` concurrency token. New rows start at `1`. For a modified entity, the Identity save pipeline retains the loaded version as EF's original value and increments the current value before issuing SQL. EF therefore generates an update predicate equivalent to `WHERE Id = @id AND Version = @originalVersion`.

If no row is affected, EF raises `DbUpdateConcurrencyException`. The module converts it to a controlled Identity concurrency error. The transaction does not retry or overwrite automatically because callers must reload and deliberately reconcile newer state.

PostgreSQL `xmin` is not used. Although convenient, it exposes an MVCC implementation detail, gives the domain a provider-specific token shape, and is less explicit in migrations and external representations.

## Conflict Handling

Expected uniqueness conflicts are recognized from `PostgresException.SqlState == "23505"` plus a stable configured constraint name. Known constraints map to specific module conflicts such as duplicate normalized login, role code, permission code, or assignment. Provider messages are not parsed.

Unknown constraint names, connection failures, timeouts, and other database errors are not converted into expected domain conflicts. They retain their failure semantics for observability and incident handling.

## Runtime Topology

AppHost adds a PostgreSQL server and one shared TicketBooking database, then passes the database reference to the API. Identity reads the resulting connection string through the conventional named connection setting.

The root Docker Compose file provides an equivalent PostgreSQL service with a persistent volume and health check. API startup depends on database health and receives the equivalent connection-string environment variable. Credentials are supplied through environment/deployment configuration; production secrets and real default passwords are not committed.

Because AppHost changes and a new required service setting are introduced, Docker Compose parity is part of this change rather than deferred work.

## Migration And Deployment

The first migration is additive: it creates schema `identity`, schema-local migration history, five tables, keys, Identity-internal foreign keys, named constraints, indexes, and concurrency columns.

Deployment order:

1. Provision or expose PostgreSQL through the selected topology.
2. Supply deployment credentials and connection configuration.
3. Apply the Identity migration.
4. Start the API with Identity module registration.
5. Verify database health and migration state before later changes write user data.

Before Identity contains durable data, rollback may use a compensating migration that drops the schema. After user data exists, rollback must preserve the schema and use forward-compatible compensating migrations or data export; dropping Identity data is not acceptable.

## Testing Strategy

### Unit Tests

Test strongly typed IDs, valid entity construction, rejected invalid values, lifecycle transitions, archived-user retention, and assignment invariants without EF Core.

### Model Metadata Tests

Inspect the EF model for schema ownership, table names, converters, lengths, keys, internal relationships, delete behaviors, named indexes, uniqueness, migration history configuration, and the `Version` concurrency token.

### PostgreSQL Integration Tests

Introduce reusable PostgreSQL container infrastructure for the integration test project. Apply real migrations to an isolated database and verify:

- all schema objects are created from an empty database;
- entities and relationships round-trip correctly;
- archived users remain stored;
- every known unique constraint raises the corresponding controlled conflict;
- unknown failures are not mislabeled;
- two contexts updating one user cause the stale writer to fail without overwriting committed state;
- migrations can be discovered and applied from the Identity assembly.

The EF in-memory provider is not acceptable for these checks because it does not reproduce PostgreSQL constraints, transaction behavior, or generated concurrency predicates.

### Architecture And Composition Tests

Architecture tests enforce that non-Identity modules cannot depend on internal persistence types. A composition test builds the API dependency-injection container with a PostgreSQL connection string and creates `IdentityDbContext`. It does not invoke Identity HTTP endpoints because this change defines none.

### Full Verification

Run repository formatting, restore, build, unit, architecture, integration, and system verification. Validate both AppHost and Docker Compose topology definitions, including connection-setting parity and absence of embedded secrets.

## Risks And Mitigations

| Risk | Mitigation |
| --- | --- |
| A write path fails to increment `Version` | Centralize incrementing in the context save pipeline and test parallel updates through real PostgreSQL. |
| Constraint names drift and break translation | Configure stable names explicitly and exercise every translation in integration tests. |
| Internal persistence leaks as module API | Keep types internal, expose only registration, and enforce dependency rules through architecture tests. |
| AppHost and Compose diverge | Treat topology parity as an acceptance requirement and verify names, health, volume, and connection settings together. |
| Early project templates provide little precedent | Make the smallest module-specific abstractions and avoid promoting Identity details into BuildingBlocks. |

## Exclusions

This change does not implement authentication, lockout behavior, password change orchestration, role/permission application use cases, `ICurrentUser`, HTTP endpoints, Audit integration, seed data, or administrator bootstrap.
