using Microsoft.EntityFrameworkCore;
using Npgsql;
using TicketBooking.Identity.Domain;

namespace TicketBooking.IntegrationTests.Identity;

[ClassDataSource<PostgreSqlFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel]
public class IdentityPostgreSqlTests(PostgreSqlFixture database)
{
    [Test]
    public async Task MigrateAsync_EmptyDatabase_CreatesIdentitySchema()
    {
        await database.ResetAndMigrateAsync();
        await using var context = database.CreateContext();

        await using var command = (NpgsqlCommand)context.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'identity'
            ORDER BY table_name;
            """;
        await context.Database.OpenConnectionAsync();

        var tables = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        await Assert.That(tables).IsEquivalentTo(
        [
            "__EFMigrationsHistory",
            "Permissions",
            "RolePermissions",
            "Roles",
            "SystemUserRoles",
            "SystemUsers",
        ]);

        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        await Assert.That(appliedMigrations).IsEquivalentTo(["20260827224854_InitialIdentity"]);
    }

    [Test]
    public async Task SaveChangesAsync_UserRolePermissionGraph_RoundTripsAllPersistenceData()
    {
        await database.ResetAndMigrateAsync();
        await using var context = database.CreateContext();
        var createdAt = new DateTimeOffset(2026, 8, 27, 10, 15, 0, TimeSpan.Zero);
        var assignedAt = new DateTimeOffset(2026, 8, 27, 10, 30, 0, TimeSpan.Zero);
        var userId = new SystemUserId(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var administratorId = new RoleId(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var publisherId = new RoleId(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var publishPermissionId = new PermissionId(Guid.Parse("44444444-4444-4444-4444-444444444444"));
        var refundPermissionId = new PermissionId(Guid.Parse("55555555-5555-5555-5555-555555555555"));

        var user = SystemUser.Create(
            userId,
            "alice",
            "ALICE",
            "argon2id-hash",
            "Alice",
            "Nguyen",
            "alice@example.test",
            "+15550100",
            createdAt,
            "identity-bootstrap");
        var administrator = Role.Create(administratorId, "administrator", "Administrator");
        var publisher = Role.Create(publisherId, "publisher", "Publisher");
        administrator.AddPermission(Permission.Create(publishPermissionId, "events.publish", "Publish events"));
        administrator.AddPermission(Permission.Create(refundPermissionId, "payments.refund", "Refund payments"));
        user.AssignRole(administrator, assignedAt, "security-admin");
        user.AssignRole(publisher, assignedAt.AddMinutes(1), "identity-admin");

        context.SystemUsers.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var reloaded = await context.SystemUsers
            .Include(candidate => candidate.Roles)
                .ThenInclude(assignment => assignment.Role)
                    .ThenInclude(role => role.Permissions)
                        .ThenInclude(association => association.Permission)
            .SingleAsync(candidate => candidate.Id == userId);

        await Assert.That(reloaded.Id).IsEqualTo(userId);
        await Assert.That(reloaded.Login).IsEqualTo("alice");
        await Assert.That(reloaded.NormalizedLogin).IsEqualTo("ALICE");
        await Assert.That(reloaded.PasswordHash).IsEqualTo("argon2id-hash");
        await Assert.That(reloaded.FirstName).IsEqualTo("Alice");
        await Assert.That(reloaded.LastName).IsEqualTo("Nguyen");
        await Assert.That(reloaded.Email).IsEqualTo("alice@example.test");
        await Assert.That(reloaded.PhoneNumber).IsEqualTo("+15550100");
        await Assert.That(reloaded.Status).IsEqualTo(SystemUserStatus.Active);
        await Assert.That(reloaded.CreatedAt).IsEqualTo(createdAt);
        await Assert.That(reloaded.CreatedBy).IsEqualTo("identity-bootstrap");
        await Assert.That(reloaded.Version).IsEqualTo(1L);
        await Assert.That(reloaded.Roles).Count().IsEqualTo(2);

        var administratorAssignment = reloaded.Roles.Single(assignment => assignment.RoleId == administratorId);
        await Assert.That(administratorAssignment.SystemUserId).IsEqualTo(userId);
        await Assert.That(administratorAssignment.AssignedAt).IsEqualTo(assignedAt);
        await Assert.That(administratorAssignment.AssignedBy).IsEqualTo("security-admin");
        await Assert.That(administratorAssignment.Role.Id).IsEqualTo(administratorId);
        await Assert.That(administratorAssignment.Role.Version).IsEqualTo(1L);
        await Assert.That(administratorAssignment.Role.Permissions.Select(item => item.PermissionId))
            .IsEquivalentTo([publishPermissionId, refundPermissionId]);
        await Assert.That(administratorAssignment.Role.Permissions.Select(item => item.Permission.Version))
            .IsEquivalentTo([1L, 1L]);

        var publisherAssignment = reloaded.Roles.Single(assignment => assignment.RoleId == publisherId);
        await Assert.That(publisherAssignment.AssignedAt).IsEqualTo(assignedAt.AddMinutes(1));
        await Assert.That(publisherAssignment.AssignedBy).IsEqualTo("identity-admin");
    }

    [Test]
    public async Task SaveChangesAsync_ArchivedUser_RemainsStoredAndIdentifiable()
    {
        await database.ResetAndMigrateAsync();
        await using var context = database.CreateContext();
        var userId = new SystemUserId(Guid.Parse("66666666-6666-6666-6666-666666666666"));
        var createdAt = new DateTimeOffset(2026, 8, 27, 11, 0, 0, TimeSpan.Zero);
        var archivedAt = new DateTimeOffset(2026, 8, 28, 9, 0, 0, TimeSpan.Zero);
        var user = SystemUser.Create(
            userId,
            "archived.user",
            "ARCHIVED.USER",
            "argon2id-archived-hash",
            "Archived",
            "User",
            "archived@example.test",
            null,
            createdAt,
            "identity-bootstrap");
        user.Archive(archivedAt, "security-admin");

        context.SystemUsers.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var reloaded = await context.SystemUsers.SingleOrDefaultAsync(candidate => candidate.Id == userId);

        await Assert.That(reloaded).IsNotNull();
        await Assert.That(reloaded!.Id).IsEqualTo(userId);
        await Assert.That(reloaded.Status).IsEqualTo(SystemUserStatus.Archived);
        await Assert.That(reloaded.UpdatedAt).IsEqualTo(archivedAt);
        await Assert.That(reloaded.UpdatedBy).IsEqualTo("security-admin");
        await Assert.That(reloaded.Version).IsEqualTo(1L);
    }
}
