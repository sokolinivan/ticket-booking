using Microsoft.EntityFrameworkCore;
using TicketBooking.Identity.Domain;
using TicketBooking.Identity.Internal.Persistence;

namespace TicketBooking.IntegrationTests.Identity;

[ClassDataSource<PostgreSqlFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel]
public class IdentityConcurrencyTests(PostgreSqlFixture database)
{
    [Test]
    public async Task SaveChangesAsync_ModifiedRoleAndPermission_AdvanceFromPreservedOriginalVersions()
    {
        await using var context = database.CreateContext();
        var role = Role.Create(new RoleId(Guid.NewGuid()), "admin", "Administrator");
        var permission = Permission.Create(new PermissionId(Guid.NewGuid()), "events.publish", "Publish events");
        context.AttachRange(role, permission);
        context.Entry(role).Property(entity => entity.Name).IsModified = true;
        context.Entry(permission).Property(entity => entity.Name).IsModified = true;
        var roleVersion = context.Entry(role).Property(entity => entity.Version);
        var permissionVersion = context.Entry(permission).Property(entity => entity.Version);
        roleVersion.OriginalValue = 7;
        permissionVersion.OriginalValue = 11;

        context.SavingChanges += (_, _) => throw new SaveInspectionCompleteException();
        var save = () => context.SaveChangesAsync();

        await Assert.That(save).Throws<SaveInspectionCompleteException>();
        await Assert.That(roleVersion.OriginalValue).IsEqualTo(7);
        await Assert.That(roleVersion.CurrentValue).IsEqualTo(8);
        await Assert.That(permissionVersion.OriginalValue).IsEqualTo(11);
        await Assert.That(permissionVersion.CurrentValue).IsEqualTo(12);
    }

    [Test]
    public async Task SaveChangesAsync_NonModifiedVersionedEntities_LeavesVersionsUntouched()
    {
        await using var context = database.CreateContext();
        var added = Role.Create(new RoleId(Guid.NewGuid()), "added", "Added");
        var unchanged = Role.Create(new RoleId(Guid.NewGuid()), "unchanged", "Unchanged");
        var deleted = Role.Create(new RoleId(Guid.NewGuid()), "deleted", "Deleted");
        context.Add(added);
        context.Attach(unchanged);
        context.Attach(deleted);
        context.Remove(deleted);

        context.SavingChanges += (_, _) => throw new SaveInspectionCompleteException();
        var save = () => context.SaveChangesAsync();

        await Assert.That(save).Throws<SaveInspectionCompleteException>();
        await Assert.That(added.Version).IsEqualTo(1);
        await Assert.That(unchanged.Version).IsEqualTo(1);
        await Assert.That(deleted.Version).IsEqualTo(1);
        await Assert.That(context.Entry(added).State).IsEqualTo(EntityState.Added);
        await Assert.That(context.Entry(unchanged).State).IsEqualTo(EntityState.Unchanged);
        await Assert.That(context.Entry(deleted).State).IsEqualTo(EntityState.Deleted);
    }

    [Test]
    public async Task SaveChangesAsync_StaleSystemUserUpdate_ReportsControlledConflictWithoutOverwrite()
    {
        await database.ResetAndMigrateAsync();
        var userId = new SystemUserId(Guid.NewGuid());
        await using (var seedContext = database.CreateContext())
        {
            seedContext.SystemUsers.Add(CreateUser(userId));
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var firstWriter = await firstContext.SystemUsers.SingleAsync(user => user.Id == userId);
        var staleWriter = await secondContext.SystemUsers.SingleAsync(user => user.Id == userId);
        var firstChangedAt = DateTimeOffset.UtcNow;
        var staleChangedAt = firstChangedAt.AddMinutes(1);
        firstWriter.Block(firstChangedAt, "first-writer");
        staleWriter.Disable(staleChangedAt, "stale-writer");

        await firstContext.SaveChangesAsync();

        IdentityPersistenceException? failure = null;
        try
        {
            await secondContext.SaveChangesAsync();
        }
        catch (IdentityPersistenceException exception)
        {
            failure = exception;
        }

        await Assert.That(firstWriter.Version).IsEqualTo(2);
        await Assert.That(failure).IsNotNull();
        await Assert.That(failure!.Conflict).IsEqualTo(IdentityPersistenceConflict.Concurrency);
        await Assert.That(failure.InnerException).IsTypeOf<DbUpdateConcurrencyException>();

        await using var verificationContext = database.CreateContext();
        var persisted = await verificationContext.SystemUsers.SingleAsync(user => user.Id == userId);
        await Assert.That(persisted.Status).IsEqualTo(SystemUserStatus.Blocked);
        await Assert.That(persisted.UpdatedBy).IsEqualTo("first-writer");
        await Assert.That(persisted.UpdatedAt).IsNotNull();
        await Assert.That(persisted.UpdatedAt!.Value).IsLessThan(staleChangedAt);
        await Assert.That(persisted.Version).IsEqualTo(2);
    }

    private static SystemUser CreateUser(SystemUserId id) => SystemUser.Create(
        id,
        "alice",
        "ALICE",
        "argon2id-hash",
        "Alice",
        "Nguyen",
        "alice@example.test",
        null,
        DateTimeOffset.UtcNow,
        "test");

    private sealed class SaveInspectionCompleteException : Exception;
}
