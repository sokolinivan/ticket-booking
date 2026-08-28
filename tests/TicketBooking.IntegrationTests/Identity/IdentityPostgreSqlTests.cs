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
        await database.ResetAsync();
        await using var context = database.CreateContext();

        await Assert.That(await GetIdentityTablesAsync(context)).IsEmpty();

        await context.Database.MigrateAsync();

        await Assert.That(await GetIdentityTablesAsync(context)).IsEquivalentTo(
        [
            "__EFMigrationsHistory",
            "Permissions",
            "RolePermissions",
            "Roles",
            "SystemUserRoles",
            "SystemUsers",
        ]);
        await Assert.That(await GetCatalogNamesAsync(
            context,
            "SELECT constraint_name FROM information_schema.table_constraints WHERE table_schema = 'identity' AND constraint_type IN ('PRIMARY KEY', 'FOREIGN KEY') ORDER BY constraint_name"))
            .IsEquivalentTo(
            [
                "FK_RolePermissions_Permissions_PermissionId",
                "FK_RolePermissions_Roles_RoleId",
                "FK_SystemUserRoles_Roles_RoleId",
                "FK_SystemUserRoles_SystemUsers_SystemUserId",
                "PK_Permissions",
                "PK_RolePermissions",
                "PK_Roles",
                "PK_SystemUserRoles",
                "PK_SystemUsers",
                "PK___EFMigrationsHistory",
            ]);
        await Assert.That(await GetCatalogNamesAsync(
            context,
            "SELECT indexname FROM pg_indexes WHERE schemaname = 'identity' ORDER BY indexname"))
            .IsEquivalentTo(
            [
                "IX_RolePermissions_PermissionId",
                "IX_SystemUserRoles_RoleId",
                "IX_SystemUsers_Email",
                "IX_SystemUsers_Status",
                "PK_Permissions",
                "PK_RolePermissions",
                "PK_Roles",
                "PK_SystemUserRoles",
                "PK_SystemUsers",
                "PK___EFMigrationsHistory",
                "UX_Permissions_Code",
                "UX_RolePermissions_RoleId_PermissionId",
                "UX_Roles_Code",
                "UX_SystemUserRoles_SystemUserId_RoleId",
                "UX_SystemUsers_NormalizedLogin",
            ]);
        await Assert.That(await GetCatalogNamesAsync(
            context,
            "SELECT table_name || '.' || column_name FROM information_schema.columns WHERE table_schema = 'identity' ORDER BY table_name, ordinal_position"))
            .IsEquivalentTo(
            [
                "Permissions.Id", "Permissions.Code", "Permissions.Name", "Permissions.Version",
                "RolePermissions.RoleId", "RolePermissions.PermissionId",
                "Roles.Id", "Roles.Code", "Roles.Name", "Roles.Version",
                "SystemUserRoles.SystemUserId", "SystemUserRoles.RoleId", "SystemUserRoles.AssignedAt", "SystemUserRoles.AssignedBy",
                "SystemUsers.Id", "SystemUsers.Login", "SystemUsers.NormalizedLogin", "SystemUsers.PasswordHash",
                "SystemUsers.FirstName", "SystemUsers.LastName", "SystemUsers.Email", "SystemUsers.PhoneNumber",
                "SystemUsers.Status", "SystemUsers.LastLoginAt", "SystemUsers.FailedLoginAttempts", "SystemUsers.CreatedAt",
                "SystemUsers.CreatedBy", "SystemUsers.UpdatedAt", "SystemUsers.UpdatedBy", "SystemUsers.Version",
                "__EFMigrationsHistory.MigrationId", "__EFMigrationsHistory.ProductVersion",
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
        await Assert.That(administratorAssignment.Role.Code).IsEqualTo("administrator");
        await Assert.That(administratorAssignment.Role.Name).IsEqualTo("Administrator");
        await Assert.That(administratorAssignment.Role.Version).IsEqualTo(1L);
        var publishAssociation = administratorAssignment.Role.Permissions.Single(item => item.PermissionId == publishPermissionId);
        await Assert.That(publishAssociation.RoleId).IsEqualTo(administratorId);
        await Assert.That(publishAssociation.Role.Id).IsEqualTo(administratorId);
        await Assert.That(publishAssociation.PermissionId).IsEqualTo(publishPermissionId);
        await Assert.That(publishAssociation.Permission.Id).IsEqualTo(publishPermissionId);
        await Assert.That(publishAssociation.Permission.Code).IsEqualTo("events.publish");
        await Assert.That(publishAssociation.Permission.Name).IsEqualTo("Publish events");
        await Assert.That(publishAssociation.Permission.Version).IsEqualTo(1L);
        await Assert.That(publishAssociation.Permission.Roles.Single().RoleId).IsEqualTo(administratorId);

        var refundAssociation = administratorAssignment.Role.Permissions.Single(item => item.PermissionId == refundPermissionId);
        await Assert.That(refundAssociation.RoleId).IsEqualTo(administratorId);
        await Assert.That(refundAssociation.Role.Id).IsEqualTo(administratorId);
        await Assert.That(refundAssociation.PermissionId).IsEqualTo(refundPermissionId);
        await Assert.That(refundAssociation.Permission.Id).IsEqualTo(refundPermissionId);
        await Assert.That(refundAssociation.Permission.Code).IsEqualTo("payments.refund");
        await Assert.That(refundAssociation.Permission.Name).IsEqualTo("Refund payments");
        await Assert.That(refundAssociation.Permission.Version).IsEqualTo(1L);
        await Assert.That(refundAssociation.Permission.Roles.Single().RoleId).IsEqualTo(administratorId);

        var publisherAssignment = reloaded.Roles.Single(assignment => assignment.RoleId == publisherId);
        await Assert.That(publisherAssignment.SystemUserId).IsEqualTo(userId);
        await Assert.That(publisherAssignment.AssignedAt).IsEqualTo(assignedAt.AddMinutes(1));
        await Assert.That(publisherAssignment.AssignedBy).IsEqualTo("identity-admin");
        await Assert.That(publisherAssignment.Role.Id).IsEqualTo(publisherId);
        await Assert.That(publisherAssignment.Role.Code).IsEqualTo("publisher");
        await Assert.That(publisherAssignment.Role.Name).IsEqualTo("Publisher");
        await Assert.That(publisherAssignment.Role.Version).IsEqualTo(1L);
        await Assert.That(publisherAssignment.Role.Permissions).IsEmpty();
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

    private static Task<List<string>> GetIdentityTablesAsync(DbContext context) => GetCatalogNamesAsync(
        context,
        "SELECT table_name FROM information_schema.tables WHERE table_schema = 'identity' ORDER BY table_name");

    private static async Task<List<string>> GetCatalogNamesAsync(DbContext context, string sql)
    {
        await context.Database.OpenConnectionAsync();
        await using var command = (NpgsqlCommand)context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        var values = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }
}
