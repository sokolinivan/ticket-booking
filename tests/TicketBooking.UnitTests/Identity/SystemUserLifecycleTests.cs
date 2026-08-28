using TicketBooking.Identity.Domain;

namespace TicketBooking.UnitTests.Identity;

public class SystemUserLifecycleTests
{
    private static readonly DateTimeOffset ChangedAt =
        new(2026, 8, 27, 15, 0, 0, TimeSpan.Zero);

    [Test]
    [MethodDataSource(nameof(AllowedTransitions))]
    public async Task Transition_AllowedSourceAndTarget_ChangesStatusAndAuditMetadata(
        SystemUserStatus source,
        SystemUserStatus target)
    {
        var user = CreateUserInStatus(source);
        var role = Role.Create(
            new RoleId(Guid.Parse("22222222-2222-2222-2222-222222222222")),
            "event-admin",
            "Event administrator");
        var assignment = user.AssignRole(role, ChangedAt.AddHours(-1), "bootstrap");
        var originalId = user.Id;
        var originalLogin = user.Login;
        var originalPasswordHash = user.PasswordHash;

        ApplyTransition(user, target, ChangedAt, "admin-1");

        await Assert.That(user.Status).IsEqualTo(target);
        await Assert.That(user.Id).IsEqualTo(originalId);
        await Assert.That(user.Login).IsEqualTo(originalLogin);
        await Assert.That(user.PasswordHash).IsEqualTo(originalPasswordHash);
        await Assert.That(user.Roles.Single()).IsSameReferenceAs(assignment);
        await Assert.That(user.UpdatedAt).IsEqualTo(ChangedAt);
        await Assert.That(user.UpdatedBy).IsEqualTo("admin-1");
    }

    [Test]
    [Arguments(SystemUserStatus.Active)]
    [Arguments(SystemUserStatus.Blocked)]
    [Arguments(SystemUserStatus.Disabled)]
    public async Task Archive_NonArchivedUser_RetainsIdentityAndRoleAssignments(SystemUserStatus source)
    {
        var user = CreateUserInStatus(source);
        var role = Role.Create(
            new RoleId(Guid.Parse("22222222-2222-2222-2222-222222222222")),
            "event-admin",
            "Event administrator");
        var assignment = user.AssignRole(role, ChangedAt.AddHours(-1), "bootstrap");
        var originalId = user.Id;
        var originalLogin = user.Login;
        var originalNormalizedLogin = user.NormalizedLogin;
        var originalPasswordHash = user.PasswordHash;

        user.Archive(ChangedAt, "admin-1");

        await Assert.That(user.Status).IsEqualTo(SystemUserStatus.Archived);
        await Assert.That(user.Id).IsEqualTo(originalId);
        await Assert.That(user.Login).IsEqualTo(originalLogin);
        await Assert.That(user.NormalizedLogin).IsEqualTo(originalNormalizedLogin);
        await Assert.That(user.PasswordHash).IsEqualTo(originalPasswordHash);
        await Assert.That(user.Roles).Count().IsEqualTo(1);
        await Assert.That(user.Roles.Single()).IsSameReferenceAs(assignment);
        await Assert.That(user.UpdatedAt).IsEqualTo(ChangedAt);
        await Assert.That(user.UpdatedBy).IsEqualTo("admin-1");
    }

    [Test]
    [Arguments(SystemUserStatus.Active)]
    [Arguments(SystemUserStatus.Blocked)]
    [Arguments(SystemUserStatus.Disabled)]
    [Arguments(SystemUserStatus.Archived)]
    public async Task Transition_FromArchived_ThrowsWithoutChangingAuditMetadata(SystemUserStatus target)
    {
        var user = CreateUser();
        user.Archive(ChangedAt, "admin-1");
        var action = () => ApplyTransition(user, target, ChangedAt.AddMinutes(1), "admin-2");

        await Assert.That(action).Throws<InvalidOperationException>();
        await Assert.That(user.Status).IsEqualTo(SystemUserStatus.Archived);
        await Assert.That(user.UpdatedAt).IsEqualTo(ChangedAt);
        await Assert.That(user.UpdatedBy).IsEqualTo("admin-1");
    }

    [Test]
    [Arguments(SystemUserStatus.Active)]
    [Arguments(SystemUserStatus.Blocked)]
    [Arguments(SystemUserStatus.Disabled)]
    public async Task Transition_ToCurrentNonArchivedStatus_ThrowsInvalidOperationException(SystemUserStatus status)
    {
        var user = CreateUserInStatus(status);
        var action = () => ApplyTransition(user, status, ChangedAt, "admin-1");

        await Assert.That(action).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Block_DisabledUser_ThrowsInvalidOperationException()
    {
        var user = CreateUserInStatus(SystemUserStatus.Disabled);
        var action = () => user.Block(ChangedAt, "admin-1");

        await Assert.That(action).Throws<InvalidOperationException>();
    }

    [Test]
    [Arguments("")]
    [Arguments(" \t")]
    public async Task Transition_MissingActor_ThrowsWithoutChangingState(string changedBy)
    {
        var user = CreateUser();
        var action = () => user.Block(ChangedAt, changedBy);

        await Assert.That(action).Throws<ArgumentException>();
        await Assert.That(user.Status).IsEqualTo(SystemUserStatus.Active);
        await Assert.That(user.UpdatedAt).IsNull();
        await Assert.That(user.UpdatedBy).IsNull();
    }

    [Test]
    public async Task Transition_DefaultTimestamp_ThrowsWithoutChangingState()
    {
        var user = CreateUser();
        var action = () => user.Block(default, "admin-1");

        await Assert.That(action).Throws<ArgumentOutOfRangeException>();
        await Assert.That(user.Status).IsEqualTo(SystemUserStatus.Active);
        await Assert.That(user.UpdatedAt).IsNull();
        await Assert.That(user.UpdatedBy).IsNull();
    }

    public static IEnumerable<(SystemUserStatus Source, SystemUserStatus Target)> AllowedTransitions()
    {
        yield return (SystemUserStatus.Active, SystemUserStatus.Blocked);
        yield return (SystemUserStatus.Active, SystemUserStatus.Disabled);
        yield return (SystemUserStatus.Blocked, SystemUserStatus.Active);
        yield return (SystemUserStatus.Blocked, SystemUserStatus.Disabled);
        yield return (SystemUserStatus.Disabled, SystemUserStatus.Active);
    }

    private static SystemUser CreateUserInStatus(SystemUserStatus status)
    {
        var user = CreateUser();

        if (status == SystemUserStatus.Blocked)
        {
            user.Block(ChangedAt.AddHours(-2), "setup");
        }
        else if (status == SystemUserStatus.Disabled)
        {
            user.Disable(ChangedAt.AddHours(-2), "setup");
        }

        return user;
    }

    private static void ApplyTransition(
        SystemUser user,
        SystemUserStatus target,
        DateTimeOffset changedAt,
        string changedBy)
    {
        switch (target)
        {
            case SystemUserStatus.Active:
                user.Activate(changedAt, changedBy);
                break;
            case SystemUserStatus.Blocked:
                user.Block(changedAt, changedBy);
                break;
            case SystemUserStatus.Disabled:
                user.Disable(changedAt, changedBy);
                break;
            case SystemUserStatus.Archived:
                user.Archive(changedAt, changedBy);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(target));
        }
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
        ChangedAt.AddDays(-1),
        "bootstrap");
}
