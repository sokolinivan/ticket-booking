## 1. Module Foundation

- [x] 1.1 Add the Identity Core project, wire it into the solution and API composition boundary, and verify `dotnet build TicketBooking.slnx` succeeds.
- [x] 1.2 Add architecture rules that prevent non-Identity modules from depending on Identity persistence types, and verify the architecture tests pass.

## 2. Domain Model

- [x] 2.1 Implement strongly typed identifiers and the `SystemUser` aggregate with required profile, lifecycle, login-tracking, audit, and concurrency state; verify focused unit tests cover construction and invalid input.
- [x] 2.2 Implement `Role`, `Permission`, `SystemUserRole`, and `RolePermission` models with stable codes and assignment metadata; verify unit tests cover valid creation and duplicate-assignment invariants.
- [x] 2.3 Implement lifecycle behavior that archives users without physical deletion, and verify unit tests demonstrate retained identity and valid status transitions.

## 3. EF Core Persistence

- [x] 3.1 Add `IdentityDbContext` and module registration against the shared PostgreSQL connection, and verify a composition test builds the API service provider and creates the context.
- [x] 3.2 Add EF Core configurations for the `identity` schema, strongly typed IDs, required fields, lengths, relationships, and `Version bigint` concurrency; verify model metadata tests assert the mappings.
- [x] 3.3 Add unique and lookup indexes for normalized login, email, status, role code, permission code, and assignment pairs; verify model metadata tests assert every required index and uniqueness setting.

## 4. Migration And Relational Verification

- [x] 4.1 Generate the initial module-owned Identity migration and verify its operations create the schema, five tables, internal keys, indexes, constraints, and concurrency column.
- [ ] 4.2 Add PostgreSQL container integration coverage for migration application, entity round trips, relationships, and archived-user retention; verify the focused integration test suite passes.
- [ ] 4.3 Add PostgreSQL integration coverage for duplicate normalized logins, duplicate role and permission codes, and duplicate assignment pairs; verify each known `23505` constraint violation maps to the corresponding controlled conflict and unknown failures remain distinguishable.
- [ ] 4.4 Add integration coverage for concurrent system-user updates and verify a stale write raises the controlled concurrency error without overwriting the committed update.

## 5. Verification And Deployment Consistency

- [ ] 5.1 Add the shared PostgreSQL resource and API reference to AppHost, create or update root Docker Compose with an equivalent PostgreSQL service, volume, health check, and connection-string setting, and verify both topologies expose equivalent runtime configuration without source-controlled secrets.
- [ ] 5.2 Run formatting, restore, build, unit, architecture, integration, and system verification commands required by repository guidance and record the exact successful commands in the verification report.
