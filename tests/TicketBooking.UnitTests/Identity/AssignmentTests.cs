using TicketBooking.Identity.Domain;

namespace TicketBooking.UnitTests.Identity;

public class AssignmentTests
{
    private static readonly DateTimeOffset AssignedAt =
        new(2026, 8, 27, 14, 30, 0, TimeSpan.Zero);

    [Test]
    public async Task AssignRole_DistinctRoles_AddsIndependentAssignmentsWithMetadata()
    {
        var user = CreateUser();
        var firstRole = Role.Create(new RoleId(Guid.Parse("22222222-2222-2222-2222-222222222222")), "event-admin", "Event administrator");
        var secondRole = Role.Create(new RoleId(Guid.Parse("33333333-3333-3333-3333-333333333333")), "box-office", "Box office");

        var firstAssignment = user.AssignRole(firstRole, AssignedAt, "admin-1");
        user.AssignRole(secondRole, AssignedAt.AddMinutes(1), "admin-2");

        await Assert.That(user.Roles).Count().IsEqualTo(2);
        await Assert.That(firstAssignment.SystemUserId).IsEqualTo(user.Id);
        await Assert.That(firstAssignment.RoleId).IsEqualTo(firstRole.Id);
        await Assert.That(firstAssignment.AssignedAt).IsEqualTo(AssignedAt);
        await Assert.That(firstAssignment.AssignedBy).IsEqualTo("admin-1");
        await Assert.That(firstAssignment.SystemUser).IsSameReferenceAs(user);
        await Assert.That(firstAssignment.Role).IsSameReferenceAs(firstRole);
    }

    [Test]
    public async Task AssignRole_DuplicateRoleId_ThrowsBeforeAddingAssignment()
    {
        var user = CreateUser();
        var role = Role.Create(new RoleId(Guid.Parse("22222222-2222-2222-2222-222222222222")), "event-admin", "Event administrator");
        var duplicateInstance = Role.Create(role.Id, "renamed", "Renamed role");
        user.AssignRole(role, AssignedAt, "admin-1");

        var action = () => user.AssignRole(duplicateInstance, AssignedAt.AddMinutes(1), "admin-2");

        await Assert.That(action).Throws<InvalidOperationException>();
        await Assert.That(user.Roles).Count().IsEqualTo(1);
    }

    [Test]
    [Arguments("")]
    [Arguments(" \t")]
    public async Task AssignRole_MissingActor_ThrowsBeforeAddingAssignment(string assignedBy)
    {
        var user = CreateUser();
        var role = Role.Create(RoleId.New(), "event-admin", "Event administrator");

        var action = () => user.AssignRole(role, AssignedAt, assignedBy);

        await Assert.That(action).Throws<ArgumentException>();
        await Assert.That(user.Roles).IsEmpty();
    }

    [Test]
    public async Task AddPermission_DistinctPermissions_AddsIndependentAssociations()
    {
        var role = Role.Create(RoleId.New(), "event-admin", "Event administrator");
        var first = Permission.Create(new PermissionId(Guid.Parse("44444444-4444-4444-4444-444444444444")), "events.publish", "Publish events");
        var second = Permission.Create(new PermissionId(Guid.Parse("55555555-5555-5555-5555-555555555555")), "events.cancel", "Cancel events");

        var association = role.AddPermission(first);
        role.AddPermission(second);

        await Assert.That(role.Permissions).Count().IsEqualTo(2);
        await Assert.That(association.RoleId).IsEqualTo(role.Id);
        await Assert.That(association.PermissionId).IsEqualTo(first.Id);
        await Assert.That(association.Role).IsSameReferenceAs(role);
        await Assert.That(association.Permission).IsSameReferenceAs(first);
    }

    [Test]
    public async Task AddPermission_DuplicatePermissionId_ThrowsBeforeAddingAssociation()
    {
        var role = Role.Create(RoleId.New(), "event-admin", "Event administrator");
        var permission = Permission.Create(new PermissionId(Guid.Parse("44444444-4444-4444-4444-444444444444")), "events.publish", "Publish events");
        var duplicateInstance = Permission.Create(permission.Id, "events.publish-renamed", "Renamed permission");
        role.AddPermission(permission);

        var action = () => role.AddPermission(duplicateInstance);

        await Assert.That(action).Throws<InvalidOperationException>();
        await Assert.That(role.Permissions).Count().IsEqualTo(1);
    }

    private static SystemUser CreateUser() => SystemUser.Create(
        new SystemUserId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
        "alice",
        "ALICE",
        "argon2-hash",
        "Alice",
        "Ng",
        "alice@example.test",
        null,
        AssignedAt.AddDays(-1),
        "bootstrap");
}
