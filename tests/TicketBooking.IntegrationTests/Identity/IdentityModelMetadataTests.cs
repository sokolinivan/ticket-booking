using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TicketBooking.Identity.Domain;
using TicketBooking.Identity.Internal.Persistence;

namespace TicketBooking.IntegrationTests.Identity;

public class IdentityModelMetadataTests
{
    [Test]
    public async Task Model_MapsIdentityTablesAndScalarPropertiesExplicitly()
    {
        await using var context = CreateContext();
        var model = context.Model;

        await AssertEntity<SystemUser>(model, "SystemUsers");
        await AssertEntity<Role>(model, "Roles");
        await AssertEntity<Permission>(model, "Permissions");
        await AssertEntity<SystemUserRole>(model, "SystemUserRoles");
        await AssertEntity<RolePermission>(model, "RolePermissions");

        await AssertProperty<SystemUser, SystemUserId>(model, nameof(SystemUser.Id), "uuid", false);
        await AssertString<SystemUser>(model, nameof(SystemUser.Login), 256, false);
        await AssertString<SystemUser>(model, nameof(SystemUser.NormalizedLogin), 256, false);
        await AssertString<SystemUser>(model, nameof(SystemUser.PasswordHash), 1024, false);
        await AssertString<SystemUser>(model, nameof(SystemUser.FirstName), 200, false);
        await AssertString<SystemUser>(model, nameof(SystemUser.LastName), 200, false);
        await AssertString<SystemUser>(model, nameof(SystemUser.Email), 320, false);
        await AssertString<SystemUser>(model, nameof(SystemUser.PhoneNumber), 64, true);
        await AssertProperty<SystemUser, SystemUserStatus>(model, nameof(SystemUser.Status), "character varying(32)", false);
        await AssertProperty<SystemUser, DateTimeOffset?>(model, nameof(SystemUser.LastLoginAt), "timestamp with time zone", true);
        await AssertProperty<SystemUser, int>(model, nameof(SystemUser.FailedLoginAttempts), "integer", false);
        await AssertProperty<SystemUser, DateTimeOffset>(model, nameof(SystemUser.CreatedAt), "timestamp with time zone", false);
        await AssertString<SystemUser>(model, nameof(SystemUser.CreatedBy), 200, false);
        await AssertProperty<SystemUser, DateTimeOffset?>(model, nameof(SystemUser.UpdatedAt), "timestamp with time zone", true);
        await AssertString<SystemUser>(model, nameof(SystemUser.UpdatedBy), 200, true);

        await AssertProperty<Role, RoleId>(model, nameof(Role.Id), "uuid", false);
        await AssertString<Role>(model, nameof(Role.Code), 128, false);
        await AssertString<Role>(model, nameof(Role.Name), 256, false);
        await AssertProperty<Permission, PermissionId>(model, nameof(Permission.Id), "uuid", false);
        await AssertString<Permission>(model, nameof(Permission.Code), 128, false);
        await AssertString<Permission>(model, nameof(Permission.Name), 256, false);

        await AssertProperty<SystemUserRole, SystemUserId>(model, nameof(SystemUserRole.SystemUserId), "uuid", false);
        await AssertProperty<SystemUserRole, RoleId>(model, nameof(SystemUserRole.RoleId), "uuid", false);
        await AssertProperty<SystemUserRole, DateTimeOffset>(model, nameof(SystemUserRole.AssignedAt), "timestamp with time zone", false);
        await AssertString<SystemUserRole>(model, nameof(SystemUserRole.AssignedBy), 200, false);
        await AssertProperty<RolePermission, RoleId>(model, nameof(RolePermission.RoleId), "uuid", false);
        await AssertProperty<RolePermission, PermissionId>(model, nameof(RolePermission.PermissionId), "uuid", false);

        await Assert.That(model.FindEntityType(typeof(SystemUser))!.FindProperty("Password")).IsNull();
    }

    [Test]
    public async Task Model_MapsKeysRelationshipsAndConcurrencyExplicitly()
    {
        await using var context = CreateContext();
        var model = context.Model;

        await AssertKey<SystemUser>(model, nameof(SystemUser.Id));
        await AssertKey<Role>(model, nameof(Role.Id));
        await AssertKey<Permission>(model, nameof(Permission.Id));
        await AssertKey<SystemUserRole>(model, nameof(SystemUserRole.SystemUserId), nameof(SystemUserRole.RoleId));
        await AssertKey<RolePermission>(model, nameof(RolePermission.RoleId), nameof(RolePermission.PermissionId));

        await AssertForeignKey<SystemUserRole, SystemUser>(model, nameof(SystemUserRole.SystemUserId));
        await AssertForeignKey<SystemUserRole, Role>(model, nameof(SystemUserRole.RoleId));
        await AssertForeignKey<RolePermission, Role>(model, nameof(RolePermission.RoleId));
        await AssertForeignKey<RolePermission, Permission>(model, nameof(RolePermission.PermissionId));

        await AssertVersion<SystemUser>(model);
        await AssertVersion<Role>(model);
        await AssertVersion<Permission>(model);
    }

    private static IdentityDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql("Host=localhost;Database=metadata;Username=postgres;Password=postgres")
            .Options;
        return new IdentityDbContext(options);
    }

    private static async Task AssertEntity<TEntity>(IModel model, string tableName)
    {
        var entity = model.FindEntityType(typeof(TEntity))!;
        await Assert.That(entity.GetTableName()).IsEqualTo(tableName);
        await Assert.That(entity.GetSchema()).IsEqualTo("identity");
    }

    private static async Task AssertString<TEntity>(IModel model, string name, int length, bool nullable)
    {
        var property = model.FindEntityType(typeof(TEntity))!.FindProperty(name)!;
        await Assert.That(property.GetColumnName()).IsEqualTo(name);
        await Assert.That(property.GetColumnType()).IsEqualTo($"character varying({length})");
        await Assert.That(property.GetMaxLength()).IsEqualTo(length);
        await Assert.That(property.IsNullable).IsEqualTo(nullable);
    }

    private static async Task AssertProperty<TEntity, TProperty>(
        IModel model,
        string name,
        string columnType,
        bool nullable)
    {
        var property = model.FindEntityType(typeof(TEntity))!.FindProperty(name)!;
        await Assert.That(property.ClrType).IsEqualTo(typeof(TProperty));
        await Assert.That(property.GetColumnName()).IsEqualTo(name);
        await Assert.That(property.GetColumnType()).IsEqualTo(columnType);
        await Assert.That(property.IsNullable).IsEqualTo(nullable);

        if (typeof(TProperty) == typeof(SystemUserId)
            || typeof(TProperty) == typeof(RoleId)
            || typeof(TProperty) == typeof(PermissionId))
        {
            await Assert.That(property.GetValueConverter()).IsNotNull();
            await Assert.That(property.GetValueComparer()).IsNotNull();
        }
    }

    private static async Task AssertKey<TEntity>(IModel model, params string[] properties)
    {
        var key = model.FindEntityType(typeof(TEntity))!.FindPrimaryKey()!;
        await Assert.That(key.Properties.Select(property => property.Name)).IsEquivalentTo(properties);
    }

    private static async Task AssertForeignKey<TDependent, TPrincipal>(IModel model, string propertyName)
    {
        var foreignKey = model.FindEntityType(typeof(TDependent))!.GetForeignKeys()
            .Single(candidate => candidate.Properties.Select(property => property.Name).SequenceEqual([propertyName]));
        await Assert.That(foreignKey.PrincipalEntityType.ClrType).IsEqualTo(typeof(TPrincipal));
        await Assert.That(foreignKey.DeleteBehavior).IsEqualTo(DeleteBehavior.Restrict);
    }

    private static async Task AssertVersion<TEntity>(IModel model)
    {
        var property = model.FindEntityType(typeof(TEntity))!.FindProperty("Version")!;
        await Assert.That(property.GetColumnType()).IsEqualTo("bigint");
        await Assert.That(property.IsNullable).IsFalse();
        await Assert.That(property.IsConcurrencyToken).IsTrue();
        await Assert.That(property.ValueGenerated).IsEqualTo(ValueGenerated.Never);
        await Assert.That(property.GetDefaultValue()).IsEqualTo(1L);
    }
}
