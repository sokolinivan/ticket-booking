using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TicketBooking.Identity.Internal.Persistence;

namespace TicketBooking.IntegrationTests.Identity;

public class IdentityMigrationMetadataTests
{
    [Test]
    public async Task InitialMigration_CreatesOnlyIdentitySchemaObjectsWithRequiredMetadata()
    {
        await using var context = new IdentityDbContext(
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseNpgsql("Host=localhost;Database=metadata;Username=postgres;Password=postgres")
                .Options);

        var migrations = context.Database.GetMigrations().ToArray();
        await Assert.That(migrations).Count().IsEqualTo(1);
        await Assert.That(migrations[0]).EndsWith("_InitialIdentity");

        var migrationsAssembly = context.GetService<IMigrationsAssembly>();
        var migrationType = migrationsAssembly.Migrations.Single().Value;
        await Assert.That(migrationType.Namespace)
            .IsEqualTo("TicketBooking.Identity.Internal.Persistence.Migrations");
        await Assert.That(migrationType.IsPublic).IsFalse();
        var migration = migrationsAssembly.CreateMigration(migrationType, context.Database.ProviderName!);

        await Assert.That(migration.UpOperations.OfType<EnsureSchemaOperation>().Select(operation => operation.Name))
            .IsEquivalentTo(["identity"]);

        var tables = migration.UpOperations.OfType<CreateTableOperation>().ToDictionary(operation => operation.Name);
        await Assert.That(tables.Keys).IsEquivalentTo(
            ["SystemUsers", "Roles", "Permissions", "SystemUserRoles", "RolePermissions"]);
        await Assert.That(tables.Values.All(table => table.Schema == "identity")).IsTrue();

        await AssertColumns(tables["SystemUsers"],
            "Id", "Login", "NormalizedLogin", "PasswordHash", "FirstName", "LastName", "Email",
            "PhoneNumber", "Status", "LastLoginAt", "FailedLoginAttempts", "CreatedAt", "CreatedBy",
            "UpdatedAt", "UpdatedBy", "Version");
        await AssertColumns(tables["Roles"], "Id", "Code", "Name", "Version");
        await AssertColumns(tables["Permissions"], "Id", "Code", "Name", "Version");
        await AssertColumns(tables["SystemUserRoles"], "SystemUserId", "RoleId", "AssignedAt", "AssignedBy");
        await AssertColumns(tables["RolePermissions"], "RoleId", "PermissionId");
        await Assert.That(tables["SystemUsers"].Columns.Any(column => column.Name == "Password")).IsFalse();
        await AssertVersion(tables["SystemUsers"]);
        await AssertVersion(tables["Roles"]);
        await AssertVersion(tables["Permissions"]);
        await Assert.That(tables.Values.All(table => table.PrimaryKey is not null)).IsTrue();

        var foreignKeys = tables.Values.SelectMany(table => table.ForeignKeys).ToArray();
        await Assert.That(foreignKeys).Count().IsEqualTo(4);
        await Assert.That(foreignKeys.All(foreignKey => foreignKey.PrincipalSchema == "identity")).IsTrue();
        await Assert.That(foreignKeys.Select(foreignKey => foreignKey.Name)).IsEquivalentTo(
            [
                "FK_SystemUserRoles_SystemUsers_SystemUserId",
                "FK_SystemUserRoles_Roles_RoleId",
                "FK_RolePermissions_Roles_RoleId",
                "FK_RolePermissions_Permissions_PermissionId",
            ]);

        var indexes = migration.UpOperations.OfType<CreateIndexOperation>().Select(operation => operation.Name);
        await Assert.That(indexes).IsEquivalentTo(
            [
                "UX_SystemUsers_NormalizedLogin",
                "IX_SystemUsers_Email",
                "IX_SystemUsers_Status",
                "UX_Roles_Code",
                "UX_Permissions_Code",
                "IX_RolePermissions_PermissionId",
                "UX_SystemUserRoles_SystemUserId_RoleId",
                "IX_SystemUserRoles_RoleId",
                "UX_RolePermissions_RoleId_PermissionId",
            ]);

        var droppedTables = migration.DownOperations.OfType<DropTableOperation>().ToArray();
        await Assert.That(droppedTables.Select(operation => operation.Name)).IsEquivalentTo(tables.Keys);
        await Assert.That(droppedTables.All(operation => operation.Schema == "identity")).IsTrue();
        await Assert.That(migration.DownOperations.OfType<DropSchemaOperation>()).IsEmpty();
    }

    private static async Task AssertColumns(CreateTableOperation table, params string[] expectedColumns)
    {
        await Assert.That(table.Columns.Select(column => column.Name)).IsEquivalentTo(expectedColumns);
    }

    private static async Task AssertVersion(CreateTableOperation table)
    {
        var version = table.Columns.Single(column => column.Name == "Version");
        await Assert.That(version.ColumnType).IsEqualTo("bigint");
        await Assert.That(version.IsNullable).IsFalse();
        await Assert.That(version.DefaultValue).IsEqualTo(1L);
    }
}
