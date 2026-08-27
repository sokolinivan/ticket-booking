using TicketBooking.Identity.Domain;

namespace TicketBooking.UnitTests.Identity;

public class RoleAndPermissionTests
{
    [Test]
    public async Task Create_ValidRole_RetainsStableValues()
    {
        var id = new RoleId(Guid.Parse("22222222-2222-2222-2222-222222222222"));

        var role = Role.Create(id, "event-admin", "Event administrator");

        await Assert.That(role.Id).IsEqualTo(id);
        await Assert.That(role.Code).IsEqualTo("event-admin");
        await Assert.That(role.Name).IsEqualTo("Event administrator");
        await Assert.That(role.Version).IsEqualTo(1L);
    }

    [Test]
    public async Task Create_ValidPermission_RetainsStableValues()
    {
        var id = new PermissionId(Guid.Parse("33333333-3333-3333-3333-333333333333"));

        var permission = Permission.Create(id, "events.publish", "Publish events");

        await Assert.That(permission.Id).IsEqualTo(id);
        await Assert.That(permission.Code).IsEqualTo("events.publish");
        await Assert.That(permission.Name).IsEqualTo("Publish events");
        await Assert.That(permission.Version).IsEqualTo(1L);
    }

    [Test]
    public async Task Create_RoleWithEmptyId_ThrowsArgumentException()
    {
        var action = () => Role.Create(new RoleId(Guid.Empty), "event-admin", "Event administrator");

        await Assert.That(action).Throws<ArgumentException>();
    }

    [Test]
    [Arguments("")]
    [Arguments(" \t")]
    public async Task Create_RoleWithMissingCode_ThrowsArgumentException(string code)
    {
        var action = () => Role.Create(RoleId.New(), code, "Event administrator");

        await Assert.That(action).Throws<ArgumentException>();
    }

    [Test]
    [Arguments("")]
    [Arguments(" \t")]
    public async Task Create_RoleWithMissingName_ThrowsArgumentException(string name)
    {
        var action = () => Role.Create(RoleId.New(), "event-admin", name);

        await Assert.That(action).Throws<ArgumentException>();
    }

    [Test]
    public async Task Create_PermissionWithEmptyId_ThrowsArgumentException()
    {
        var action = () => Permission.Create(new PermissionId(Guid.Empty), "events.publish", "Publish events");

        await Assert.That(action).Throws<ArgumentException>();
    }

    [Test]
    [Arguments("")]
    [Arguments(" \t")]
    public async Task Create_PermissionWithMissingCode_ThrowsArgumentException(string code)
    {
        var action = () => Permission.Create(PermissionId.New(), code, "Publish events");

        await Assert.That(action).Throws<ArgumentException>();
    }

    [Test]
    [Arguments("")]
    [Arguments(" \t")]
    public async Task Create_PermissionWithMissingName_ThrowsArgumentException(string name)
    {
        var action = () => Permission.Create(PermissionId.New(), "events.publish", name);

        await Assert.That(action).Throws<ArgumentException>();
    }
}
