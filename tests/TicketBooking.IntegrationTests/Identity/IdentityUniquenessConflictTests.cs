using Microsoft.EntityFrameworkCore;
using Npgsql;
using TicketBooking.Identity.Domain;
using TicketBooking.Identity.Internal.Persistence;

namespace TicketBooking.IntegrationTests.Identity;

[ClassDataSource<PostgreSqlFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel]
public class IdentityUniquenessConflictTests(PostgreSqlFixture database)
{
    [Test]
    public async Task SaveChangesAsync_DuplicateNormalizedLogin_ReportsControlledConflict()
    {
        await database.ResetAndMigrateAsync();
        await SaveUserAsync(CreateUser(Guid.NewGuid(), "alice", "ALICE"));
        await using var context = database.CreateContext();
        context.SystemUsers.Add(CreateUser(Guid.NewGuid(), "alice.other", "ALICE"));

        await AssertKnownConflictAsync(
            () => context.SaveChangesAsync(),
            IdentityPersistenceConflict.DuplicateNormalizedLogin);
    }

    [Test]
    public async Task SaveChangesAsync_DuplicateRoleCode_ReportsControlledConflict()
    {
        await database.ResetAndMigrateAsync();
        await SaveRoleAsync(Role.Create(new RoleId(Guid.NewGuid()), "administrator", "Administrator"));
        await using var context = database.CreateContext();
        context.Roles.Add(Role.Create(new RoleId(Guid.NewGuid()), "administrator", "Other administrator"));

        await AssertKnownConflictAsync(
            () => context.SaveChangesAsync(),
            IdentityPersistenceConflict.DuplicateRoleCode);
    }

    [Test]
    public async Task SaveChangesAsync_DuplicatePermissionCode_ReportsControlledConflict()
    {
        await database.ResetAndMigrateAsync();
        await SavePermissionAsync(Permission.Create(new PermissionId(Guid.NewGuid()), "events.publish", "Publish events"));
        await using var context = database.CreateContext();
        context.Permissions.Add(Permission.Create(new PermissionId(Guid.NewGuid()), "events.publish", "Publish other events"));

        await AssertKnownConflictAsync(
            () => context.SaveChangesAsync(),
            IdentityPersistenceConflict.DuplicatePermissionCode);
    }

    [Test]
    public async Task SaveChangesAsync_DuplicateSystemUserRole_ReportsControlledConflict()
    {
        await database.ResetAndMigrateAsync();
        var userId = new SystemUserId(Guid.NewGuid());
        var roleId = new RoleId(Guid.NewGuid());
        await using (var seed = database.CreateContext())
        {
            var user = CreateUser(userId.Value, "alice", "ALICE");
            var role = Role.Create(roleId, "administrator", "Administrator");
            user.AssignRole(role, DateTimeOffset.UtcNow, "test");
            seed.SystemUsers.Add(user);
            await seed.SaveChangesAsync();
        }

        await using var context = database.CreateContext();
        var persistedUser = await context.SystemUsers.SingleAsync(candidate => candidate.Id == userId);
        var persistedRole = await context.Roles.SingleAsync(candidate => candidate.Id == roleId);
        persistedUser.AssignRole(persistedRole, DateTimeOffset.UtcNow, "test");

        await AssertKnownConflictAsync(
            () => context.SaveChangesAsync(),
            IdentityPersistenceConflict.DuplicateSystemUserRole);
    }

    [Test]
    public async Task SaveChangesAsync_DuplicateRolePermission_ReportsControlledConflict()
    {
        await database.ResetAndMigrateAsync();
        var roleId = new RoleId(Guid.NewGuid());
        var permissionId = new PermissionId(Guid.NewGuid());
        await using (var seed = database.CreateContext())
        {
            var role = Role.Create(roleId, "administrator", "Administrator");
            role.AddPermission(Permission.Create(permissionId, "events.publish", "Publish events"));
            seed.Roles.Add(role);
            await seed.SaveChangesAsync();
        }

        await using var context = database.CreateContext();
        var persistedRole = await context.Roles.SingleAsync(candidate => candidate.Id == roleId);
        var persistedPermission = await context.Permissions.SingleAsync(candidate => candidate.Id == permissionId);
        persistedRole.AddPermission(persistedPermission);

        await AssertKnownConflictAsync(
            () => context.SaveChangesAsync(),
            IdentityPersistenceConflict.DuplicateRolePermission);
    }

    [Test]
    public async Task SaveChanges_DuplicateRoleCode_ReportsControlledConflict()
    {
        await database.ResetAndMigrateAsync();
        await SaveRoleAsync(Role.Create(new RoleId(Guid.NewGuid()), "administrator", "Administrator"));
        using var context = database.CreateContext();
        context.Roles.Add(Role.Create(new RoleId(Guid.NewGuid()), "administrator", "Other administrator"));

        var exception = AssertKnownConflict(
            context.SaveChanges,
            IdentityPersistenceConflict.DuplicateRoleCode);

        await Assert.That(exception.InnerException).IsTypeOf<PostgresException>();
    }

    [Test]
    public async Task SaveChangesAsync_UnknownUniqueConstraint_PreservesDbUpdateException()
    {
        await database.ResetAndMigrateAsync();
        var roleId = new RoleId(Guid.NewGuid());
        await SaveRoleAsync(Role.Create(roleId, "administrator", "Administrator"));
        await using var context = database.CreateContext();
        context.Roles.Add(Role.Create(roleId, "publisher", "Publisher"));

        Exception? failure = null;
        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        await Assert.That(failure).IsTypeOf<DbUpdateException>();
        await Assert.That(failure).IsNotTypeOf<IdentityPersistenceException>();
        await Assert.That(failure!.InnerException).IsTypeOf<PostgresException>();
        await Assert.That(((PostgresException)failure.InnerException!).ConstraintName)
            .IsEqualTo(IdentityConstraintNames.RolesPrimaryKey);
    }

    private async Task SaveUserAsync(SystemUser user)
    {
        await using var context = database.CreateContext();
        context.SystemUsers.Add(user);
        await context.SaveChangesAsync();
    }

    private async Task SaveRoleAsync(Role role)
    {
        await using var context = database.CreateContext();
        context.Roles.Add(role);
        await context.SaveChangesAsync();
    }

    private async Task SavePermissionAsync(Permission permission)
    {
        await using var context = database.CreateContext();
        context.Permissions.Add(permission);
        await context.SaveChangesAsync();
    }

    private static SystemUser CreateUser(Guid id, string login, string normalizedLogin) => SystemUser.Create(
        new SystemUserId(id),
        login,
        normalizedLogin,
        "argon2id-hash",
        "Alice",
        "Nguyen",
        $"{login}@example.test",
        null,
        DateTimeOffset.UtcNow,
        "test");

    private static async Task AssertKnownConflictAsync(
        Func<Task> action,
        IdentityPersistenceConflict expectedConflict)
    {
        IdentityPersistenceException? failure = null;
        try
        {
            await action();
        }
        catch (IdentityPersistenceException exception)
        {
            failure = exception;
        }

        await Assert.That(failure).IsNotNull();
        await Assert.That(failure!.Conflict).IsEqualTo(expectedConflict);
        await Assert.That(failure.InnerException).IsTypeOf<PostgresException>();
        await Assert.That(failure.Message).DoesNotContain(failure.InnerException!.Message);
    }

    private static IdentityPersistenceException AssertKnownConflict(
        Func<int> action,
        IdentityPersistenceConflict expectedConflict)
    {
        try
        {
            action();
        }
        catch (IdentityPersistenceException exception)
        {
            if (exception.Conflict != expectedConflict)
            {
                throw new InvalidOperationException("The persistence conflict did not match the expected value.", exception);
            }

            if (exception.Message.Contains(exception.InnerException!.Message, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The controlled message exposed provider text.", exception);
            }

            return exception;
        }

        throw new InvalidOperationException("The expected persistence conflict was not thrown.");
    }
}
