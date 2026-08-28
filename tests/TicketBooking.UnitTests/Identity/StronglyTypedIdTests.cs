using TicketBooking.Identity.Domain;

namespace TicketBooking.UnitTests.Identity;

public class StronglyTypedIdTests
{
    [Test]
    public async Task New_CalledTwice_ReturnsDistinctNonEmptySystemUserIds()
    {
        var first = SystemUserId.New();
        var second = SystemUserId.New();

        await Assert.That(first.Value).IsNotEqualTo(Guid.Empty);
        await Assert.That(first).IsNotEqualTo(second);
    }

    [Test]
    public async Task New_CalledForEachType_ReturnsItsEntitySpecificType()
    {
        await Assert.That(SystemUserId.New()).IsTypeOf<SystemUserId>();
        await Assert.That(RoleId.New()).IsTypeOf<RoleId>();
        await Assert.That(PermissionId.New()).IsTypeOf<PermissionId>();
    }

    [Test]
    public async Task Constructed_WithSameBackingValue_ComparesEqualWithinEachType()
    {
        var value = Guid.Parse("11111111-1111-1111-1111-111111111111");

        await Assert.That(new SystemUserId(value)).IsEqualTo(new SystemUserId(value));
        await Assert.That(new RoleId(value)).IsEqualTo(new RoleId(value));
        await Assert.That(new PermissionId(value)).IsEqualTo(new PermissionId(value));
    }
}
