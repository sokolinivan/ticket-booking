## Context

The backend currently contains empty Companies Contracts and Core project shells, a shared BuildingBlocks project, one API host, and Aspire service defaults. The centrally managed packages already select PostgreSQL, Npgsql, and Aspire PostgreSQL. Identity must become a new module boundary inside the modular monolith while establishing the shared PostgreSQL deployment. See `proposal.md` for motivation and `specs/identity/domain-and-persistence/spec.md` for required behavior.

## Goals / Non-Goals

**Goals:**

- Establish an Identity-owned domain model that later login and authorization changes can extend without reshaping persistence.
- Keep persistence, migrations, and database ownership within the Identity module.
- Make uniqueness and concurrency guarantees enforceable by both domain behavior and PostgreSQL.
- Follow the repository's existing module, dependency, testing, and composition patterns.

**Non-Goals:**

- Implement password verification, lockout workflows, token issuance, or current-user resolution.
- Implement commands, queries, HTTP endpoints, Audit consumers, seed data, or administrator bootstrap.
- Add physical foreign keys from other module schemas to Identity tables.

## Decisions

### Model Identity as a separate Core module

Add an Identity Core project that owns domain entities and internal persistence. It exposes only module registration to the API composition root. A separate Contracts project is unnecessary in this first change because no public inter-module behavior is introduced yet; contracts can be added by the authorization/application change when concrete consumers exist. This is preferred over placing Identity types in the API or BuildingBlocks because those alternatives would erase module ownership.

### Use strongly typed identifiers backed by Guid

Each aggregate or independently referenced entity uses a dedicated readonly record-struct identifier backed by `Guid`. EF Core value converters map these IDs to database-native values. This prevents accidental identifier mixing while retaining straightforward storage and test setup. Raw `Guid` properties were rejected because they permit cross-entity assignment mistakes.

### Treat SystemUser as the aggregate root

`SystemUser` owns identity lifecycle state, login-attempt tracking fields, personal/contact data, and user-role membership behavior. `Role` and `Permission` remain independently managed Identity entities because their codes and role-permission composition have lifecycles separate from a user. Join records are explicit models where assignment metadata is required.

### Use explicit lifecycle status instead of soft-delete flags

`Active`, `Blocked`, `Disabled`, and `Archived` are represented by an enum persisted as a stable string value. Archived records remain queryable and retain identity references. A generic `IsDeleted` flag was rejected because it cannot represent operationally distinct blocked and disabled states.

### Give Identity its own DbContext and migrations

`IdentityDbContext` exposes only Identity-owned sets, applies `identity` as the default schema, and has an Identity-specific migration history. The API composition root registers it against the existing shared database connection. Reusing a global application DbContext was rejected because it would permit direct cross-module queries and couple migration ownership.

### Enforce invariants at domain and database boundaries

Domain constructors and mutation methods reject missing required values and invalid state. Database mappings add required lengths, unique indexes, composite keys or indexes, and Identity-internal foreign keys. Login normalization is supplied as an already normalized invariant in this persistence-focused change; the later application change owns normalization policy and orchestration.

### Use an explicit PostgreSQL bigint version for optimistic concurrency

Mutable aggregate rows use an explicit `Version bigint` column configured as the EF Core concurrency token. New records start at version 1, and the context increments the original version before each mutable update so EF includes the prior value in the update predicate. PostgreSQL `xmin` was rejected because it exposes an MVCC implementation detail and creates Npgsql-specific domain/API semantics.

### Keep AppHost and Docker Compose deployment-equivalent

AppHost owns a shared PostgreSQL server and database resource and injects the database reference into the API. The root Docker Compose configuration provides the equivalent PostgreSQL service, persistent volume, health check, and API connection-string setting without embedding deployment secrets. This follows the explicit repository delivery requirement and avoids an Aspire-only local topology.

### Translate only known persistence conflicts

The module translates EF concurrency failures into a controlled Identity concurrency error. PostgreSQL unique violations are recognized by SQLSTATE `23505` and known constraint names and translated into specific conflicts. Unknown database failures retain their original failure semantics instead of being mislabeled as domain errors.

### Test persistence against PostgreSQL

Unit tests cover entity invariants without EF Core. Integration tests establish reusable PostgreSQL container infrastructure and apply Identity migrations before verifying schema, uniqueness, relationships, and stale-write behavior. EF Core's in-memory provider is rejected because it does not reproduce PostgreSQL constraints, transaction behavior, or concurrency predicates. A composition test builds the API service provider and creates `IdentityDbContext`; it does not exercise Identity HTTP endpoints because none are introduced.

## Risks / Trade-offs

- [The existing module pattern may not yet include EF Core conventions] -> Keep Identity configuration local and add only reusable primitives to BuildingBlocks when an existing shared abstraction clearly applies.
- [String enum persistence can break if enum members are renamed] -> Treat persisted status names as stable database values and cover mappings with migration and integration tests.
- [A shared physical database can tempt direct cross-module access] -> Keep `IdentityDbContext` internal to the module registration surface and enforce dependencies with architecture tests.
- [Application-managed Version can be missed on an update path] -> Centralize incrementing in the Identity context save pipeline and verify it with metadata and parallel-update integration tests.
- [Constraint-name coupling can drift during migration changes] -> Define stable constraint names in mappings and test every translated conflict against PostgreSQL.

## Migration Plan

1. Add shared PostgreSQL topology to AppHost and equivalent root Docker Compose configuration.
2. Add the Identity project and register it in the solution and API composition root.
3. Add domain models, mappings, and `IdentityDbContext`.
4. Generate the initial Identity migration using the module-owned migration assembly and schema-local migration history.
5. Apply the migration in integration tests and verify all relational guarantees.
6. Deploy PostgreSQL configuration and the additive migration before any later change writes Identity data.

Rollback is safe before Identity data is used: remove the additive Identity schema through a compensating migration or restore the database. Once production identities exist, rollback must preserve or export those records rather than dropping the schema.
