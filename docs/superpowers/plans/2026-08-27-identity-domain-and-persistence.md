---
change: identity-domain-and-persistence
design-doc: docs/superpowers/specs/2026-08-27-identity-domain-and-persistence-design.md
base-ref: 15bdc95546713a8af47ffd2a9962f99e648d05cd
---

# Identity Domain And Persistence Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Establish an Identity-owned domain and PostgreSQL persistence boundary with durable users, roles, permissions, controlled conflicts, optimistic concurrency, migrations, and equivalent Aspire/Compose topology.

**Architecture:** Add one `TicketBooking.Identity.Core` module whose public surface is module registration and whose domain, EF Core context, configurations, migrations, and error translation remain module-owned. Register it in the API against the shared `ticketbooking` PostgreSQL connection, enforce boundaries through architecture tests, and validate relational behavior against a real PostgreSQL container rather than an in-memory provider.

**Tech Stack:** .NET 10, C# 14, EF Core 10.0.11, Npgsql 10.0.3, PostgreSQL, Aspire 13.5.3, Testcontainers for .NET, TUnit 1.6.28, ArchUnitNET 0.13.4, Docker Compose.

**Spec:** `docs/superpowers/specs/2026-08-27-identity-domain-and-persistence-design.md` and canonical delta `docs/openspec/changes/identity-domain-and-persistence/specs/identity/domain-and-persistence/spec.md`

## Global Constraints

- Implement only `docs/openspec/changes/identity-domain-and-persistence/tasks.md`; do not add authentication, password verification/change orchestration, lockout behavior, authorization use cases, `ICurrentUser`, HTTP endpoints, Audit integration, seed data, or administrator bootstrap.
- Use `identity` as the default schema and `__EFMigrationsHistory` in the `identity` schema; all physical foreign keys must remain between Identity-owned tables.
- Persist only password hashes, never plaintext passwords; normal lifecycle operations archive users and never physically delete them.
- Use dedicated `readonly record struct` identifiers backed by `Guid` and explicit EF value converters/comparers.
- Persist `SystemUserStatus` as the stable strings `Active`, `Blocked`, `Disabled`, and `Archived`.
- Use an explicit `Version bigint` concurrency token initialized to `1`; increment it centrally before updates and never retry stale writes automatically.
- Translate only PostgreSQL SQLSTATE `23505` violations whose configured constraint names are known, plus `DbUpdateConcurrencyException`; never parse provider messages or relabel unknown database failures.
- Keep `IdentityDbContext`, EF configurations, migrations, and persistence translation internal. Expose only `AddIdentityModule(IServiceCollection, IConfiguration)` to the API composition root; tests may access internals through `InternalsVisibleTo`.
- Use centrally managed package versions: package references in `.csproj` files have no versions, and new versions belong in `Directory.Packages.props`.
- Tests use TUnit executables through `dotnet run`; do not use `dotnet test`. Every narrow filter includes `--minimum-expected-tests 1`.
- Integration tests use real PostgreSQL and require Docker; do not use EF Core InMemory or SQLite as a substitute.
- Source-controlled AppHost and Compose files must not contain production credentials, real default passwords, or committed connection strings.
- All implementation and commands run from `/home/bean/Projects/work/ticket-booking/.worktrees/identity-domain-and-persistence` unless a step gives another working directory.

## File Map

- `src/Backend/Modules/TicketBooking.Identity.Core/TicketBooking.Identity.Core.csproj`: module dependencies and friend-test assemblies.
- `src/Backend/Modules/TicketBooking.Identity.Core/DependencyInjection.cs`: sole production composition entry, `AddIdentityModule`.
- `src/Backend/Modules/TicketBooking.Identity.Core/Domain/*.cs`: typed IDs, status, aggregate/entities, assignment models, and domain validation.
- `src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/IdentityDbContext.cs`: Identity sets, save pipeline, version increment, and controlled exception translation.
- `src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/IdentityPersistenceException.cs`: controlled conflict kind and exception contract.
- `src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/IdentityConstraintNames.cs`: stable names shared by mappings and translation.
- `src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/Configurations/*.cs`: one focused EF mapping per entity.
- `src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/Migrations/*`: generated module-owned initial migration and snapshot.
- `tests/TicketBooking.UnitTests/Identity/*.cs`: pure domain construction, assignment, and lifecycle tests.
- `tests/TicketBooking.ArchitectureTests/IdentityModuleArchitectureTests.cs`: module persistence-boundary checks.
- `tests/TicketBooking.IntegrationTests/Identity/PostgreSqlFixture.cs`: reusable PostgreSQL Testcontainer and per-test database reset/migration support.
- `tests/TicketBooking.IntegrationTests/Identity/*.cs`: composition, model metadata, migrations, round trips, conflicts, and concurrency tests.
- `src/Aspire/TicketBooking.AppHost/AppHost.cs` and its `.csproj`: shared PostgreSQL server/database and API reference.
- `compose.yaml`: equivalent API/PostgreSQL topology, volume, health check, and `ConnectionStrings__ticketbooking` wiring.
- `docs/superpowers/reports/2026-08-27-identity-domain-and-persistence-verify.md`: exact task 5.2 verification evidence.

---

### Task 1: Add The Identity Module Foundation (tasks 1.1 and 1.2)

**Files:**
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/TicketBooking.Identity.Core.csproj`
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/DependencyInjection.cs`
- Create: `tests/TicketBooking.ArchitectureTests/IdentityModuleArchitectureTests.cs`
- Modify: `TicketBooking.slnx`
- Modify: `src/Backend/TicketBooking.Api/TicketBooking.Api.csproj`
- Modify: `src/Backend/TicketBooking.Api/Program.cs`
- Modify: `tests/TicketBooking.ArchitectureTests/TicketBooking.ArchitectureTests.csproj`

**Interfaces:**
- Consumes: `WebApplicationBuilder.Services`, `WebApplicationBuilder.Configuration`, and the existing Companies module assemblies.
- Produces: public `TicketBooking.Identity.DependencyInjection.AddIdentityModule(this IServiceCollection services, IConfiguration configuration) : IServiceCollection`; an Identity Core assembly addressable by later tasks; architecture rules forbidding non-Identity module references to `TicketBooking.Identity.Internal.Persistence`.

- [x] **Step 1: Write the failing architecture tests**

Create `IdentityModuleArchitectureTests.cs` with tests that load Identity Core, Companies Core, Companies Contracts, BuildingBlocks, and API assemblies via ArchUnitNET, then assert that types outside the Identity assembly do not depend on types in namespace `TicketBooking.Identity.Internal.Persistence` and that persistence types are not public. Use explicit test names:

```csharp
[Test]
public async Task NonIdentityModules_DependingOnIdentityPersistence_IsForbidden()
{
    var result = Types().That().ResideOutsideOfAssembly(typeof(DependencyInjection).Assembly)
        .Should().NotDependOnAny(Types().That().ResideInNamespace("TicketBooking.Identity.Internal.Persistence", true))
        .Check(_architecture);

    await Assert.That(result.HasNoViolations).IsTrue();
}

[Test]
public async Task IdentityPersistenceTypes_PublicVisibility_IsForbidden()
{
    var result = Types().That().ResideInNamespace("TicketBooking.Identity.Internal.Persistence", true)
        .Should().NotBePublic().Check(_architecture);

    await Assert.That(result.HasNoViolations).IsTrue();
}
```

- [x] **Step 2: Add project references needed to compile the tests and run them red**

Add project references from ArchitectureTests to Identity Core, Companies Core, Companies Contracts, BuildingBlocks, and API. Add the Identity project path to `TicketBooking.slnx` before creating persistence types so the first test fails because the expected namespace/module boundary is not yet represented, not because no test is discovered.

Run:

```bash
dotnet restore TicketBooking.slnx
dotnet build TicketBooking.slnx --no-restore
dotnet run --project tests/TicketBooking.ArchitectureTests --no-build -- --treenode-filter "/*/*/IdentityModuleArchitectureTests/*" --minimum-expected-tests 1
```

Expected: the architecture test executable runs, with at least one assertion failing until the Identity public/internal surface is established.

- [x] **Step 3: Add the minimal module project and composition boundary**

Create the SDK project with references to `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, and `Microsoft.Extensions.Options.ConfigurationExtensions`, plus friend access for the three focused test assemblies:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" />
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
</ItemGroup>
<ItemGroup>
  <InternalsVisibleTo Include="TicketBooking.ArchitectureTests" />
  <InternalsVisibleTo Include="TicketBooking.IntegrationTests" />
  <InternalsVisibleTo Include="TicketBooking.UnitTests" />
</ItemGroup>
```

Add the project under `/src/Backend/Modules/Identity/` in `TicketBooking.slnx`, reference it from API, and add the initial extension without persistence registration yet:

```csharp
namespace TicketBooking.Identity;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        return services;
    }
}
```

Call `builder.Services.AddIdentityModule(builder.Configuration);` in API `Program.cs`. Do not add endpoints.

- [x] **Step 4: Build and run the architecture tests green**

Run:

```bash
dotnet build TicketBooking.slnx --no-restore
dotnet run --project tests/TicketBooking.ArchitectureTests --no-build -- --treenode-filter "/*/*/IdentityModuleArchitectureTests/*" --minimum-expected-tests 1
```

Expected: build succeeds and all Identity architecture tests pass.

- [x] **Step 5: Commit the module boundary**

```bash
git add TicketBooking.slnx src/Backend/Modules/TicketBooking.Identity.Core src/Backend/TicketBooking.Api tests/TicketBooking.ArchitectureTests
git commit -m "feat(identity): add module boundary"
```

### Task 2: Implement Strongly Typed IDs And SystemUser (task 2.1)

**Files:**
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Domain/SystemUserId.cs`
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Domain/RoleId.cs`
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Domain/PermissionId.cs`
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Domain/SystemUserStatus.cs`
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Domain/SystemUser.cs`
- Create: `tests/TicketBooking.UnitTests/Identity/StronglyTypedIdTests.cs`
- Create: `tests/TicketBooking.UnitTests/Identity/SystemUserTests.cs`
- Modify: `tests/TicketBooking.UnitTests/TicketBooking.UnitTests.csproj`

**Interfaces:**
- Consumes: no EF Core API; domain construction accepts already-normalized login and an already-produced password hash.
- Produces: `readonly record struct SystemUserId(Guid Value)`, `RoleId(Guid Value)`, `PermissionId(Guid Value)` with `New()` factories; `SystemUserStatus`; and `SystemUser.Create(...)` with immutable identity/audit creation state and internal EF-compatible constructors/setters.

- [x] **Step 1: Reference Identity Core and write failing ID tests**

Reference Identity Core from UnitTests. Test that `New()` yields non-empty IDs, the three ID types cannot be confused by their API, and equal backing values compare equal within one type:

```csharp
[Test]
public async Task New_CalledTwice_ReturnsDistinctNonEmptyValues()
{
    var first = SystemUserId.New();
    var second = SystemUserId.New();
    await Assert.That(first.Value).IsNotEqualTo(Guid.Empty);
    await Assert.That(first).IsNotEqualTo(second);
}
```

Run `dotnet run --project tests/TicketBooking.UnitTests -- --treenode-filter "/*/*/StronglyTypedIdTests/*" --minimum-expected-tests 1` and expect compilation failure because the ID types do not exist.

- [x] **Step 2: Implement the three minimal ID types and rerun the focused test**

Use the same exact shape for each entity-specific type:

```csharp
public readonly record struct SystemUserId(Guid Value)
{
    public static SystemUserId New() => new(Guid.NewGuid());
}
```

Run the same focused command and expect PASS.

- [x] **Step 3: Write failing SystemUser construction and validation tests**

Define a test factory using fixed UTC timestamps and assert every required field is retained: `Id`, `Login`, `NormalizedLogin`, `PasswordHash`, `FirstName`, `LastName`, `Email`, `PhoneNumber`, `Status`, `LastLoginAt`, `FailedLoginAttempts`, `CreatedAt`, `CreatedBy`, nullable `UpdatedAt`/`UpdatedBy`, and `Version`. Add parameterized tests that reject empty/whitespace login, normalized login, password hash, first name, last name, email, and creator; reject empty IDs; reject negative failed-attempt counts; and verify new users are `Active`, have no last login/update metadata, zero failed attempts, and version `1`.

```csharp
[Test]
public async Task Create_ValidRequiredValues_CreatesActiveVersionOneUser()
{
    var user = SystemUser.Create(
        new SystemUserId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
        "alice", "ALICE", "argon2-hash", "Alice", "Ng",
        "alice@example.test", "+15550000001", CreatedAt, "bootstrap");

    await Assert.That(user.Status).IsEqualTo(SystemUserStatus.Active);
    await Assert.That(user.Version).IsEqualTo(1L);
    await Assert.That(user.PasswordHash).IsEqualTo("argon2-hash");
    await Assert.That(user.LastLoginAt).IsNull();
}
```

Run the SystemUser class filter and expect compilation failure.

- [x] **Step 4: Implement the minimal aggregate construction contract**

Add `SystemUserStatus` with exactly the four stable members. Implement `SystemUser.Create` and a private parameterless constructor for EF. Keep collection navigation and lifecycle mutation for later tasks. Centralize required-string guards in a private method inside `SystemUser`; do not create a BuildingBlocks abstraction. Never accept or expose plaintext password state.

- [x] **Step 5: Run all focused domain tests green**

```bash
dotnet build tests/TicketBooking.UnitTests/TicketBooking.UnitTests.csproj
dotnet run --project tests/TicketBooking.UnitTests --no-build -- --treenode-filter "/*/*/StronglyTypedIdTests/*" --minimum-expected-tests 1
dotnet run --project tests/TicketBooking.UnitTests --no-build -- --treenode-filter "/*/*/SystemUserTests/*" --minimum-expected-tests 1
```

Expected: all construction and invalid-input cases pass.

- [x] **Step 6: Commit the core user model**

```bash
git add src/Backend/Modules/TicketBooking.Identity.Core/Domain tests/TicketBooking.UnitTests
git commit -m "feat(identity): add system user domain model"
```

### Task 3: Implement Roles, Permissions, And Assignments (task 2.2)

**Files:**
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Domain/Role.cs`
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Domain/Permission.cs`
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Domain/SystemUserRole.cs`
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Domain/RolePermission.cs`
- Modify: `src/Backend/Modules/TicketBooking.Identity.Core/Domain/SystemUser.cs`
- Create: `tests/TicketBooking.UnitTests/Identity/RoleAndPermissionTests.cs`
- Create: `tests/TicketBooking.UnitTests/Identity/AssignmentTests.cs`

**Interfaces:**
- Consumes: typed IDs and `SystemUser` from Task 2.
- Produces: `Role.Create(RoleId id, string code, string name)`, `Permission.Create(PermissionId id, string code, string name)`, `SystemUser.AssignRole(Role role, DateTimeOffset assignedAt, string assignedBy)`, and `Role.AddPermission(Permission permission)`. The first produces `SystemUserRole` assignment metadata; both methods reject duplicate IDs before persistence.

- [x] **Step 1: Write failing entity tests**

Test valid code/name retention and rejection of empty ID, code, or name for Role and Permission. Codes are supplied as stable values; do not invent normalization policy in this change.

```csharp
[Test]
public async Task Create_ValidRole_RetainsStableCode()
{
    var role = Role.Create(new RoleId(RoleGuid), "event-admin", "Event administrator");
    await Assert.That(role.Code).IsEqualTo("event-admin");
}
```

Run the class filter and expect compilation failure.

- [x] **Step 2: Implement Role and Permission minimally and rerun green**

Give each an EF-compatible private constructor, read-only public properties, and private mutable assignment collections exposed as `IReadOnlyCollection<T>`. Initialize `Version = 1` for these independently mutable records so the global mutable-row concurrency rule can be mapped consistently.

- [x] **Step 3: Write failing assignment invariant tests**

Test multiple distinct roles per user, assignment timestamp/actor retention, duplicate role rejection, multiple distinct permissions per role, duplicate permission rejection, and required assignment actor. Assert duplicate methods throw `InvalidOperationException` before another join object is added.

```csharp
user.AssignRole(role, AssignedAt, "admin-1");
var duplicate = () => user.AssignRole(role, AssignedAt.AddMinutes(1), "admin-2");
await Assert.That(duplicate).Throws<InvalidOperationException>();
await Assert.That(user.Roles).Count().IsEqualTo(1);
```

- [x] **Step 4: Implement explicit joins and aggregate methods**

`SystemUserRole` stores `SystemUserId`, `RoleId`, `AssignedAt`, and `AssignedBy`, plus navigations. `RolePermission` stores `RoleId` and `PermissionId`, plus navigations; no assignment metadata is added because neither task nor spec requires it. Check duplicate IDs against the backing collections before constructing a join.

- [x] **Step 5: Run all role/permission/assignment tests green**

```bash
dotnet build tests/TicketBooking.UnitTests/TicketBooking.UnitTests.csproj
dotnet run --project tests/TicketBooking.UnitTests --no-build -- --treenode-filter "/*/*/RoleAndPermissionTests/*" --minimum-expected-tests 1
dotnet run --project tests/TicketBooking.UnitTests --no-build -- --treenode-filter "/*/*/AssignmentTests/*" --minimum-expected-tests 1
```

- [x] **Step 6: Commit independent records and joins**

```bash
git add src/Backend/Modules/TicketBooking.Identity.Core/Domain tests/TicketBooking.UnitTests/Identity
git commit -m "feat(identity): add roles permissions and assignments"
```

### Task 4: Implement User Lifecycle Without Physical Deletion (task 2.3)

**Files:**
- Modify: `src/Backend/Modules/TicketBooking.Identity.Core/Domain/SystemUser.cs`
- Create: `tests/TicketBooking.UnitTests/Identity/SystemUserLifecycleTests.cs`

**Interfaces:**
- Consumes: `SystemUserStatus` and aggregate state from Task 2.
- Produces: `Block(DateTimeOffset changedAt, string changedBy)`, `Disable(...)`, `Activate(...)`, and `Archive(...)`; archive is terminal, updates audit metadata, and never removes identity state.

- [x] **Step 1: Write the lifecycle transition matrix as failing tests**

Use method data to cover `Active -> Blocked`, `Active -> Disabled`, `Blocked -> Active`, `Blocked -> Disabled`, `Disabled -> Active`, any non-archived state to `Archived`, and rejection of every transition out of `Archived`. Also assert transitions preserve `Id`, login, password hash, and role assignments and update `UpdatedAt`/`UpdatedBy`.

```csharp
[Test]
public async Task Archive_ActiveUser_RetainsIdentityAndMarksArchived()
{
    var user = CreateUser();
    var originalId = user.Id;
    user.Archive(ChangedAt, "admin-1");
    await Assert.That(user.Status).IsEqualTo(SystemUserStatus.Archived);
    await Assert.That(user.Id).IsEqualTo(originalId);
}
```

- [x] **Step 2: Run the focused lifecycle tests red**

Run `dotnet run --project tests/TicketBooking.UnitTests -- --treenode-filter "/*/*/SystemUserLifecycleTests/*" --minimum-expected-tests 1`; expect compile failures for missing methods.

- [x] **Step 3: Implement only the tested transition rules**

Use one private transition method that rejects archived source state, validates actor/time inputs, changes only status and audit update fields, and contains no delete method or `IsDeleted` flag. Make repeated transition to the current non-archived status an invalid transition rather than silently hiding caller mistakes.

- [x] **Step 4: Run the complete Identity unit suite**

```bash
dotnet build tests/TicketBooking.UnitTests/TicketBooking.UnitTests.csproj
dotnet run --project tests/TicketBooking.UnitTests --no-build -- --treenode-filter "/*/*/Identity/*" --minimum-expected-tests 1
```

Expected: all Identity domain tests pass.

- [x] **Step 5: Commit lifecycle behavior**

```bash
git add src/Backend/Modules/TicketBooking.Identity.Core/Domain/SystemUser.cs tests/TicketBooking.UnitTests/Identity/SystemUserLifecycleTests.cs
git commit -m "feat(identity): add retained user lifecycle"
```

### Task 5: Add IdentityDbContext And API Composition (task 3.1)

**Files:**
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/IdentityDbContext.cs`
- Modify: `src/Backend/Modules/TicketBooking.Identity.Core/DependencyInjection.cs`
- Create: `tests/TicketBooking.IntegrationTests/Identity/IdentityCompositionTests.cs`
- Modify: `tests/TicketBooking.IntegrationTests/TicketBooking.IntegrationTests.csproj`

**Interfaces:**
- Consumes: configuration key `ConnectionStrings:ticketbooking` and all five domain entity types.
- Produces: internal `IdentityDbContext(DbContextOptions<IdentityDbContext>)`, five internal `DbSet` properties, migrations assembly set to Identity Core, and schema-local history table; DI registration resolvable from a scope.

- [x] **Step 1: Add IntegrationTests references and write the failing composition test**

Reference Identity Core and add `Microsoft.Extensions.DependencyInjection` plus configuration support. Build a fresh `ServiceCollection`, supply an in-memory configuration containing a syntactically valid PostgreSQL connection string, invoke registration, build the provider, create a scope, and resolve `IdentityDbContext` without opening a connection.

```csharp
services.AddIdentityModule(configuration);
await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
await Assert.That(context).IsNotNull();
```

- [x] **Step 2: Run the composition test red**

Run `dotnet run --project tests/TicketBooking.IntegrationTests -- --treenode-filter "/*/*/IdentityCompositionTests/*" --minimum-expected-tests 1`; expect compilation failure because the context is absent.

- [x] **Step 3: Implement context registration and ownership settings**

Register with `services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName).MigrationsHistoryTable("__EFMigrationsHistory", "identity")))`. Fail startup registration with a clear `InvalidOperationException` when `ConnectionStrings:ticketbooking` is missing. Define DbSets for `SystemUser`, `Role`, `Permission`, `SystemUserRole`, and `RolePermission`; call `modelBuilder.HasDefaultSchema("identity")` and `ApplyConfigurationsFromAssembly` in `OnModelCreating`.

- [x] **Step 4: Run composition and API build green**

```bash
dotnet build TicketBooking.slnx --no-restore
dotnet run --project tests/TicketBooking.IntegrationTests --no-build -- --treenode-filter "/*/*/IdentityCompositionTests/*" --minimum-expected-tests 1
```

- [x] **Step 5: Commit context composition**

```bash
git add src/Backend/Modules/TicketBooking.Identity.Core tests/TicketBooking.IntegrationTests
git commit -m "feat(identity): register identity database context"
```

### Task 6: Configure Entity Mapping And Concurrency Metadata (task 3.2)

**Files:**
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/Configurations/SystemUserConfiguration.cs`
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/Configurations/RoleConfiguration.cs`
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/Configurations/PermissionConfiguration.cs`
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/Configurations/SystemUserRoleConfiguration.cs`
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/Configurations/RolePermissionConfiguration.cs`
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/Configurations/StronglyTypedIdConverters.cs`
- Create: `tests/TicketBooking.IntegrationTests/Identity/IdentityModelMetadataTests.cs`

**Interfaces:**
- Consumes: context and domain types from Tasks 2-5.
- Produces: explicit table/schema/key/column/FK/delete behavior/converter/comparer/string-length/status/version metadata for all five tables.

- [ ] **Step 1: Write failing model metadata tests for ownership and scalar mappings**

Instantiate the context with Npgsql options and inspect `context.Model`. Assert exact tables in schema `identity`; typed IDs map to `uuid` and have converters/comparers; required fields are non-null; statuses convert to strings; and lengths are explicit: login/normalized login `256`, password hash `1024`, first/last name `200`, email `320`, phone `64`, actor/code/name fields `200`/`128`/`256` as applicable. Assert no plaintext-password property exists.

- [ ] **Step 2: Add failing relationship and concurrency metadata assertions**

Assert Identity-only FKs, composite join primary keys `(SystemUserId, RoleId)` and `(RoleId, PermissionId)`, `DeleteBehavior.Restrict`, no cross-module principal entity, and `Version` mapped as required PostgreSQL `bigint`, concurrency token, non-generated value with default `1`.

Run the class filter and expect failures because conventions do not satisfy explicit mappings.

- [ ] **Step 3: Implement converters/comparers and focused configurations**

Use one converter and comparer pair per ID type, for example:

```csharp
internal sealed class SystemUserIdConverter()
    : ValueConverter<SystemUserId, Guid>(id => id.Value, value => new SystemUserId(value));

internal sealed class SystemUserIdComparer()
    : ValueComparer<SystemUserId>((left, right) => left == right,
        id => id.GetHashCode(), id => new SystemUserId(id.Value));
```

Configure every column, constraint, FK, navigation backing field, and delete behavior explicitly. Use `HasConversion<string>()` for status and `IsConcurrencyToken()` for version. Do not add indexes yet; Task 7 owns index behavior.

- [ ] **Step 4: Run metadata tests green**

```bash
dotnet build tests/TicketBooking.IntegrationTests/TicketBooking.IntegrationTests.csproj
dotnet run --project tests/TicketBooking.IntegrationTests --no-build -- --treenode-filter "/*/*/IdentityModelMetadataTests/*" --minimum-expected-tests 1
```

- [ ] **Step 5: Commit explicit mappings**

```bash
git add src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/Configurations tests/TicketBooking.IntegrationTests/Identity/IdentityModelMetadataTests.cs
git commit -m "feat(identity): map identity persistence model"
```

### Task 7: Add Stable Constraints And Required Indexes (task 3.3)

**Files:**
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/IdentityConstraintNames.cs`
- Modify: all five files under `Internal/Persistence/Configurations/`
- Modify: `tests/TicketBooking.IntegrationTests/Identity/IdentityModelMetadataTests.cs`

**Interfaces:**
- Consumes: entity configurations from Task 6.
- Produces: stable constants and database names `UX_SystemUsers_NormalizedLogin`, `IX_SystemUsers_Email`, `IX_SystemUsers_Status`, `UX_Roles_Code`, `UX_Permissions_Code`, `UX_SystemUserRoles_SystemUserId_RoleId`, and `UX_RolePermissions_RoleId_PermissionId`; stable PK/FK names used by migration assertions.

- [ ] **Step 1: Add failing index metadata assertions**

For each expected property sequence, assert index name and uniqueness. Ensure Email and Status are non-unique and every named `UX_` index is unique. Assert no extra uniqueness requirement on unnormalized Login or Email.

- [ ] **Step 2: Run the metadata class red**

Run the Task 6 class filter; expect missing-index failures.

- [ ] **Step 3: Add stable constraint constants and mapping calls**

Reference constants from `HasDatabaseName(...)`; use the same constants later for exception translation. Name PKs and internal FKs explicitly with `PK_*` and `FK_*` constants. Composite join PKs enforce the pair invariant; retain the explicitly named unique pair indexes required by tasks/spec even if PostgreSQL also gets uniqueness from the PK.

- [ ] **Step 4: Run metadata tests green**

```bash
dotnet build tests/TicketBooking.IntegrationTests/TicketBooking.IntegrationTests.csproj
dotnet run --project tests/TicketBooking.IntegrationTests --no-build -- --treenode-filter "/*/*/IdentityModelMetadataTests/*" --minimum-expected-tests 1
```

- [ ] **Step 5: Commit stable relational names**

```bash
git add src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence tests/TicketBooking.IntegrationTests/Identity/IdentityModelMetadataTests.cs
git commit -m "feat(identity): add identity indexes and constraints"
```

### Task 8: Generate And Verify The Initial Migration (task 4.1)

**Files:**
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/IdentityDesignTimeDbContextFactory.cs`
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/Migrations/<timestamp>_InitialIdentity.cs`
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/Migrations/<timestamp>_InitialIdentity.Designer.cs`
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/Migrations/IdentityDbContextModelSnapshot.cs`
- Create: `tests/TicketBooking.IntegrationTests/Identity/IdentityMigrationMetadataTests.cs`
- Modify: `src/Backend/Modules/TicketBooking.Identity.Core/TicketBooking.Identity.Core.csproj`

**Interfaces:**
- Consumes: completed EF model and `dotnet ef` tooling.
- Produces: discoverable `InitialIdentity` migration owned by Identity Core; `Up` creates schema and five tables with all keys/FKs/indexes/version columns, while `Down` drops only Identity-owned objects.

- [ ] **Step 1: Add design dependency and write a failing migration-discovery test**

Reference `Microsoft.EntityFrameworkCore.Design` with `PrivateAssets="all"`. Assert `context.Database.GetMigrations()` contains exactly the initial Identity migration and use `IMigrationsAssembly`/migration operations to assert `EnsureSchemaOperation("identity")`, five `CreateTableOperation`s, required columns, internal FKs, and all named indexes.

- [ ] **Step 2: Run the migration metadata test red**

Run `dotnet run --project tests/TicketBooking.IntegrationTests -- --treenode-filter "/*/*/IdentityMigrationMetadataTests/*" --minimum-expected-tests 1`; expect no migration discovered.

- [ ] **Step 3: Add the design-time factory and generate the migration**

The factory must use a non-secret design-time placeholder connection only to build metadata and repeat the runtime migrations assembly/history settings. Generate, do not hand-author, the migration:

```bash
dotnet tool restore
dotnet ef migrations add InitialIdentity --project src/Backend/Modules/TicketBooking.Identity.Core --context IdentityDbContext --output-dir Internal/Persistence/Migrations
```

Inspect generated `Up`, `Down`, designer, and snapshot. Confirm it creates exactly `SystemUsers`, `Roles`, `Permissions`, `SystemUserRoles`, and `RolePermissions` in `identity`, with no foreign key outside that schema and no accidental plaintext password column.

- [ ] **Step 4: Run model and migration metadata tests green**

```bash
dotnet build tests/TicketBooking.IntegrationTests/TicketBooking.IntegrationTests.csproj
dotnet run --project tests/TicketBooking.IntegrationTests --no-build -- --treenode-filter "/*/*/IdentityModelMetadataTests/*" --minimum-expected-tests 1
dotnet run --project tests/TicketBooking.IntegrationTests --no-build -- --treenode-filter "/*/*/IdentityMigrationMetadataTests/*" --minimum-expected-tests 1
```

- [ ] **Step 5: Commit generated migration as one coherent artifact**

```bash
git add src/Backend/Modules/TicketBooking.Identity.Core tests/TicketBooking.IntegrationTests/Identity/IdentityMigrationMetadataTests.cs
git commit -m "feat(identity): add initial identity migration"
```

### Task 9: Add PostgreSQL Fixture, Migration, And Round-Trip Coverage (task 4.2)

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `tests/TicketBooking.IntegrationTests/TicketBooking.IntegrationTests.csproj`
- Create: `tests/TicketBooking.IntegrationTests/Identity/PostgreSqlFixture.cs`
- Create: `tests/TicketBooking.IntegrationTests/Identity/IdentityPostgreSqlTests.cs`

**Interfaces:**
- Consumes: Docker, a pinned `Testcontainers.PostgreSql` package, and the initial migration.
- Produces: a per-test-session PostgreSQL container fixture, isolated clean database/schema state per test, `CreateContext()` using the fixture connection, and proof of migration application, round trips, relationships, and archived retention.

- [ ] **Step 1: Add the centrally pinned Testcontainers dependency and fixture skeleton**

Add the current compatible `Testcontainers.PostgreSql` version to `Directory.Packages.props` and an unversioned test-project reference. Implement TUnit async initialization/disposal around `PostgreSqlBuilder`, with a non-production test-only database/user/password generated or fixed solely inside test process configuration. Expose `ResetAndMigrateAsync()` and `CreateContext()`; do not commit an `.env` file.

- [ ] **Step 2: Write failing migration application tests**

Apply `Database.MigrateAsync()` to an empty database. Query PostgreSQL catalogs to assert schema `identity`, all five tables, and `identity.__EFMigrationsHistory`; assert `GetAppliedMigrationsAsync()` matches the generated migration.

- [ ] **Step 3: Run the migration application test red**

```bash
dotnet run --project tests/TicketBooking.IntegrationTests -- --treenode-filter "/*/*/IdentityPostgreSqlTests/MigrateAsync_EmptyDatabase_CreatesIdentitySchema" --minimum-expected-tests 1
```

Expected: initial fixture/test failure before migration setup is completed. If Docker is unavailable, stop and report the environmental blocker rather than changing providers.

- [ ] **Step 4: Complete fixture migration setup and make migration test green**

Reset only the isolated test database between tests, create a new context, and call `MigrateAsync`. Keep migrations in the Identity assembly and history in `identity`.

- [ ] **Step 5: Write failing round-trip, relationship, and archived-retention tests**

Persist one user with two roles, one role with two permissions, and assignment metadata; clear the change tracker; reload using explicit includes; assert scalar fields, typed IDs, joins, actors/timestamps, and version `1`. In a separate test archive a user, save, clear, reload by ID, and assert the row still exists with `Archived` status.

- [ ] **Step 6: Make only persistence-access adjustments needed by the tests**

Adjust internal collection backing-field mapping or EF materialization constructors as necessary. Do not add repositories, application services, endpoints, or seed data.

- [ ] **Step 7: Run focused PostgreSQL coverage green**

```bash
dotnet build tests/TicketBooking.IntegrationTests/TicketBooking.IntegrationTests.csproj
dotnet run --project tests/TicketBooking.IntegrationTests --no-build -- --treenode-filter "/*/*/IdentityPostgreSqlTests/*" --minimum-expected-tests 1
```

- [ ] **Step 8: Commit PostgreSQL integration infrastructure and behavior**

```bash
git add Directory.Packages.props tests/TicketBooking.IntegrationTests src/Backend/Modules/TicketBooking.Identity.Core
git commit -m "test(identity): verify postgres persistence"
```

### Task 10: Translate Known PostgreSQL Uniqueness Conflicts (task 4.3)

**Files:**
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/IdentityPersistenceConflict.cs`
- Create: `src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/IdentityPersistenceException.cs`
- Modify: `src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/IdentityDbContext.cs`
- Create: `tests/TicketBooking.IntegrationTests/Identity/IdentityUniquenessConflictTests.cs`

**Interfaces:**
- Consumes: SQLSTATE `23505`, `PostgresException.ConstraintName`, and names from `IdentityConstraintNames`.
- Produces: controlled conflict values `DuplicateNormalizedLogin`, `DuplicateRoleCode`, `DuplicatePermissionCode`, `DuplicateSystemUserRole`, and `DuplicateRolePermission`; unknown constraint/provider failures retain their original exception semantics.

- [ ] **Step 1: Write one failing PostgreSQL test for every known constraint**

Insert conflicts from separate context instances so domain collection guards do not preempt database validation. For each expected unique constraint, call `SaveChangesAsync()` and assert `IdentityPersistenceException.Conflict` equals its specific enum member and retains the provider exception as `InnerException` without exposing provider text as the controlled message.

- [ ] **Step 2: Write the failing unknown-failure distinction test**

Cause a PostgreSQL failure not matching a known unique constraint, such as violating a required mapped column via direct SQL, and assert it is not surfaced as `IdentityPersistenceException`. Also unit-test the internal translator with a synthetic/derived `DbUpdateException` path if needed to cover an unknown `23505` constraint name without adding a production-only constraint.

- [ ] **Step 3: Run conflict tests red**

Run `dotnet run --project tests/TicketBooking.IntegrationTests -- --treenode-filter "/*/*/IdentityUniquenessConflictTests/*" --minimum-expected-tests 1`; expect raw `DbUpdateException` failures for known conflicts.

- [ ] **Step 4: Implement narrow translation around both save overloads**

Override synchronous and asynchronous `SaveChanges` entry points used by the module. Catch `DbUpdateException` only when `InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres` and `ConstraintName` matches a known constant. Throw the controlled exception for those exact names; use `throw;` for all unknown names and all other database errors. Do not parse `Message`, `Detail`, or table/column text.

- [ ] **Step 5: Run known and unknown conflict tests green**

```bash
dotnet build tests/TicketBooking.IntegrationTests/TicketBooking.IntegrationTests.csproj
dotnet run --project tests/TicketBooking.IntegrationTests --no-build -- --treenode-filter "/*/*/IdentityUniquenessConflictTests/*" --minimum-expected-tests 1
```

- [ ] **Step 6: Commit conflict translation**

```bash
git add src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence tests/TicketBooking.IntegrationTests/Identity/IdentityUniquenessConflictTests.cs
git commit -m "feat(identity): translate known persistence conflicts"
```

### Task 11: Enforce Optimistic Concurrency (task 4.4)

**Files:**
- Modify: `src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/IdentityPersistenceConflict.cs`
- Modify: `src/Backend/Modules/TicketBooking.Identity.Core/Internal/Persistence/IdentityDbContext.cs`
- Create: `tests/TicketBooking.IntegrationTests/Identity/IdentityConcurrencyTests.cs`

**Interfaces:**
- Consumes: `Version` concurrency metadata and `DbUpdateConcurrencyException`.
- Produces: central version advancement for modified SystemUser/Role/Permission rows and controlled `IdentityPersistenceConflict.Concurrency`; stale state is never retried or overwritten.

- [ ] **Step 1: Write the failing two-context stale-write test**

Seed a user, load it into two separate contexts, update distinct profile/audit fields through a narrowly scoped domain mutation method if one is required for the test, save the first, then save the second. Assert first save changes version `1 -> 2`; second save throws controlled concurrency conflict; a third context sees only the first writer’s values and version `2`.

```csharp
await firstContext.SaveChangesAsync();
var staleSave = () => secondContext.SaveChangesAsync();
await Assert.That(staleSave).Throws<IdentityPersistenceException>()
    .WithProperty(exception => exception.Conflict)
    .EqualTo(IdentityPersistenceConflict.Concurrency);
```

- [ ] **Step 2: Run the concurrency test red**

Run `dotnet run --project tests/TicketBooking.IntegrationTests -- --treenode-filter "/*/*/IdentityConcurrencyTests/*" --minimum-expected-tests 1`; expect version not to advance or raw concurrency behavior.

- [ ] **Step 3: Implement central version advancement before SQL generation**

Before base save, iterate modified entries implementing the module’s mutable-version contract (keep that contract internal if introduced). Leave `entry.Property(nameof(...Version)).OriginalValue` unchanged and set current value to checked `original + 1`. Do not increment Added/Unchanged/Deleted rows. Catch `DbUpdateConcurrencyException` after the update returns zero rows and translate it to `IdentityPersistenceConflict.Concurrency`; never retry.

- [ ] **Step 4: Verify stale write and committed-state retention green**

```bash
dotnet build tests/TicketBooking.IntegrationTests/TicketBooking.IntegrationTests.csproj
dotnet run --project tests/TicketBooking.IntegrationTests --no-build -- --treenode-filter "/*/*/IdentityConcurrencyTests/*" --minimum-expected-tests 1
dotnet run --project tests/TicketBooking.IntegrationTests --no-build -- --treenode-filter "/*/*/IdentityPostgreSqlTests/*" --minimum-expected-tests 1
```

- [ ] **Step 5: Commit concurrency behavior**

```bash
git add src/Backend/Modules/TicketBooking.Identity.Core tests/TicketBooking.IntegrationTests/Identity/IdentityConcurrencyTests.cs
git commit -m "feat(identity): enforce optimistic concurrency"
```

### Task 12: Add Equivalent Aspire And Docker Compose PostgreSQL Topologies (task 5.1)

**Files:**
- Modify: `src/Aspire/TicketBooking.AppHost/TicketBooking.AppHost.csproj`
- Modify: `src/Aspire/TicketBooking.AppHost/AppHost.cs`
- Create: `compose.yaml`
- Create: `tests/TicketBooking.ArchitectureTests/IdentityTopologyTests.cs`

**Interfaces:**
- Consumes: Aspire PostgreSQL hosting API 13.5.3 and API configuration key `ConnectionStrings:ticketbooking`.
- Produces: Aspire resource names `postgres` and `ticketbooking`; Compose service `postgres`, named persistent volume, health check, API health dependency, and equivalent `ConnectionStrings__ticketbooking` environment setting supplied from deployment variables.

- [ ] **Step 1: Confirm the installed Aspire API before planning syntax is implemented**

Run from `src/Aspire/TicketBooking.AppHost`:

```bash
aspire docs search "PostgreSQL AddPostgres AddDatabase WithReference WaitFor"
aspire docs api search "Aspire.Hosting.PostgreSQL"
```

Use the 13.5.x signatures returned by the installed tooling rather than copying older Aspire examples.

- [ ] **Step 2: Write failing static topology parity tests**

Read `AppHost.cs` and `compose.yaml` as configuration artifacts. Assert both define PostgreSQL, shared database name `ticketbooking`, API dependency/reference, Compose named volume, `pg_isready` health check, and `ConnectionStrings__ticketbooking`; scan both plus AppHost settings for prohibited literal production passwords/connection strings.

- [ ] **Step 3: Run topology tests red**

Run `dotnet run --project tests/TicketBooking.ArchitectureTests -- --treenode-filter "/*/*/IdentityTopologyTests/*" --minimum-expected-tests 1`; expect failure because PostgreSQL and Compose are absent.

- [ ] **Step 4: Add the Aspire PostgreSQL resource and API reference**

Add unversioned `Aspire.Hosting.PostgreSQL` package reference. Using confirmed APIs, model this graph:

```csharp
var postgres = builder.AddPostgres("postgres");
var database = postgres.AddDatabase("ticketbooking");
builder.AddProject<Projects.TicketBooking_Api>("ticketbooking-api")
    .WithReference(database)
    .WaitFor(database);
```

Retain both existing Vite resources unchanged. Use AppHost user secrets/deployment parameters for credentials; do not add literal credentials to source.

- [ ] **Step 5: Add equivalent root Compose topology**

Create `compose.yaml` with `postgres` using deployment-provided `${POSTGRES_USER:?required}`, `${POSTGRES_PASSWORD:?required}`, and `${POSTGRES_DB:-ticketbooking}` values, `pg_isready` health check, a named `postgres-data` volume, and API `depends_on.postgres.condition: service_healthy`. Set API `ConnectionStrings__ticketbooking` from interpolated deployment values, not a committed credential. Build API from its existing Dockerfile and do not add unrelated frontend services.

- [ ] **Step 6: Validate topology syntax and tests**

```bash
POSTGRES_USER=test POSTGRES_PASSWORD=test POSTGRES_DB=ticketbooking docker compose config --quiet
dotnet build TicketBooking.slnx --no-restore
dotnet run --project tests/TicketBooking.ArchitectureTests --no-build -- --treenode-filter "/*/*/IdentityTopologyTests/*" --minimum-expected-tests 1
```

Expected: Compose resolves with injected test-only values, solution builds, and parity/security assertions pass.

- [ ] **Step 7: Commit topology parity**

```bash
git add src/Aspire/TicketBooking.AppHost compose.yaml tests/TicketBooking.ArchitectureTests/IdentityTopologyTests.cs
git commit -m "feat(identity): add shared postgres topology"
```

### Task 13: Run Full Verification And Record Evidence (task 5.2)

**Files:**
- Create: `docs/superpowers/reports/2026-08-27-identity-domain-and-persistence-verify.md`
- Modify only if verification exposes a scoped defect: files introduced or modified by Tasks 1-12.

**Interfaces:**
- Consumes: completed scoped implementation and repository verification guidance.
- Produces: exact successful commands, outcome summaries, test counts where available, environment prerequisites, and explicit skipped/unavailable checks in the verification report.

- [ ] **Step 1: Restore and verify formatting**

```bash
dotnet restore TicketBooking.slnx
dotnet format TicketBooking.slnx --verify-no-changes --no-restore
```

Expected: both exit zero. If format reports changes, run `dotnet format TicketBooking.slnx --no-restore`, inspect only scoped formatting edits, then rerun verification mode.

- [ ] **Step 2: Build the complete solution with warnings as errors**

```bash
dotnet build TicketBooking.slnx --no-restore
```

Expected: zero warnings and zero errors.

- [ ] **Step 3: Run unit, architecture, and PostgreSQL integration executables**

```bash
dotnet run --project tests/TicketBooking.UnitTests --no-build -- --minimum-expected-tests 1
dotnet run --project tests/TicketBooking.ArchitectureTests --no-build -- --minimum-expected-tests 1
dotnet run --project tests/TicketBooking.IntegrationTests --no-build -- --minimum-expected-tests 1
```

Expected: all pass; IntegrationTests applies real migrations against Docker PostgreSQL.

- [ ] **Step 4: Run existing system verification**

```bash
dotnet run --project tests/TicketBooking.SystemTests --no-build
```

Expected: all pre-existing system tests pass.

- [ ] **Step 5: Verify Compose and AppHost resource topology**

```bash
POSTGRES_USER=test POSTGRES_PASSWORD=test POSTGRES_DB=ticketbooking docker compose config --quiet
aspire start --isolated --non-interactive
aspire ps --include-hidden
aspire stop --non-interactive
```

Run Aspire commands from `src/Aspire/TicketBooking.AppHost`. Confirm `postgres`, `ticketbooking`, and `ticketbooking-api` appear and API waits for the healthy database. The known Vite/npm installer limitation does not justify changing frontend package management; record it if it prevents unrelated frontend resources from becoming healthy.

- [ ] **Step 6: Run repository frontend checks required by full guidance**

```bash
pnpm --dir src/Frontend/public-web lint
pnpm --dir src/Frontend/public-web build
pnpm --dir src/Frontend/backoffice-web lint
pnpm --dir src/Frontend/backoffice-web build
```

Expected: all exit zero; do not run root `pnpm install` and do not commit `dist/` output.

- [ ] **Step 7: Write the verification report with exact evidence**

Create the report with sections `Scope`, `Environment`, `Commands`, `Results`, and `Skipped Or Unavailable`. Copy commands exactly as actually run and state pass/fail plus relevant test counts. Do not claim an unavailable Docker/Aspire/frontend check passed; state the blocker and leave task 5.2 incomplete until required verification can run successfully.

- [ ] **Step 8: Recheck scope and secrets before the final commit**

```bash
git diff --check
git status --short
git diff --stat 15bdc95546713a8af47ffd2a9962f99e648d05cd
```

Inspect the diff for plaintext passwords, committed connection strings, `.env`, generated `bin/`, `obj/`, `dist/`, database files, and changes outside tasks 1.1-5.2. Remove generated artifacts without reverting unrelated user work.

- [ ] **Step 9: Commit verification evidence**

```bash
git add docs/superpowers/reports/2026-08-27-identity-domain-and-persistence-verify.md
git commit -m "docs(identity): record persistence verification"
```

## Self-Review Checklist

- [ ] Tasks 1-13 map only to tasks 1.1-5.2; no excluded authentication, HTTP, application-use-case, Audit, seed, or bootstrap behavior is introduced.
- [ ] Domain coverage includes durable required user state, stable typed IDs, four lifecycle statuses, archived retention, independent roles/permissions, assignment metadata, and duplicate assignment guards.
- [ ] Persistence coverage includes the `identity` schema, exactly five tables, schema-local migration history, explicit converters/comparers, internal FKs, stable constraints/indexes, and `Version bigint` concurrency.
- [ ] PostgreSQL coverage includes empty-database migration, round trips, relationships, archive retention, every known uniqueness conflict, unknown failure distinction, and stale-write non-overwrite behavior.
- [ ] Composition/deployment coverage includes API registration, Aspire database reference, Compose health/volume/dependency/configuration parity, and no source-controlled secrets.
- [ ] Every behavior task follows red-green sequencing and ends with a focused executable verification command and coherent commit.
- [ ] Type and naming consistency is preserved: `ticketbooking`, `identity`, `SystemUserId`, `RoleId`, `PermissionId`, `SystemUserStatus`, `IdentityDbContext`, `IdentityPersistenceException`, `IdentityPersistenceConflict`, and all `IdentityConstraintNames` are used consistently.
