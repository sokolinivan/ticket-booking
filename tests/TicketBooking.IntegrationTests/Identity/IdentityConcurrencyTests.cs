using Microsoft.EntityFrameworkCore;
using TicketBooking.Identity.Domain;
using TicketBooking.Identity.Internal.Persistence;

namespace TicketBooking.IntegrationTests.Identity;

[ClassDataSource<PostgreSqlFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel]
public class IdentityConcurrencyTests(PostgreSqlFixture database)
{
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
}
