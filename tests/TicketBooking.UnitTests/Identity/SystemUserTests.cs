using TicketBooking.Identity.Domain;

namespace TicketBooking.UnitTests.Identity;

public class SystemUserTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task Create_ValidRequiredValues_RetainsAllCreationState()
    {
        var id = new SystemUserId(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        var user = SystemUser.Create(
            id,
            "alice",
            "ALICE",
            "argon2-hash",
            "Alice",
            "Ng",
            "alice@example.test",
            "+15550000001",
            CreatedAt,
            "bootstrap");

        await Assert.That(user.Id).IsEqualTo(id);
        await Assert.That(user.Login).IsEqualTo("alice");
        await Assert.That(user.NormalizedLogin).IsEqualTo("ALICE");
        await Assert.That(user.PasswordHash).IsEqualTo("argon2-hash");
        await Assert.That(user.FirstName).IsEqualTo("Alice");
        await Assert.That(user.LastName).IsEqualTo("Ng");
        await Assert.That(user.Email).IsEqualTo("alice@example.test");
        await Assert.That(user.PhoneNumber).IsEqualTo("+15550000001");
        await Assert.That(user.Status).IsEqualTo(SystemUserStatus.Active);
        await Assert.That(user.LastLoginAt).IsNull();
        await Assert.That(user.FailedLoginAttempts).IsEqualTo(0);
        await Assert.That(user.CreatedAt).IsEqualTo(CreatedAt);
        await Assert.That(user.CreatedBy).IsEqualTo("bootstrap");
        await Assert.That(user.UpdatedAt).IsNull();
        await Assert.That(user.UpdatedBy).IsNull();
        await Assert.That(user.Version).IsEqualTo(1L);
    }

    [Test]
    [Arguments("login")]
    [Arguments("normalizedLogin")]
    [Arguments("passwordHash")]
    [Arguments("firstName")]
    [Arguments("lastName")]
    [Arguments("email")]
    [Arguments("createdBy")]
    public async Task Create_MissingRequiredString_ThrowsArgumentException(string parameter)
    {
        foreach (var invalidValue in new[] { string.Empty, " \t" })
        {
            var values = new Dictionary<string, string>
            {
                ["login"] = "alice",
                ["normalizedLogin"] = "ALICE",
                ["passwordHash"] = "argon2-hash",
                ["firstName"] = "Alice",
                ["lastName"] = "Ng",
                ["email"] = "alice@example.test",
                ["createdBy"] = "bootstrap",
            };
            values[parameter] = invalidValue;

            var action = () => SystemUser.Create(
                new SystemUserId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
                values["login"],
                values["normalizedLogin"],
                values["passwordHash"],
                values["firstName"],
                values["lastName"],
                values["email"],
                null,
                CreatedAt,
                values["createdBy"]);

            await Assert.That(action).Throws<ArgumentException>();
        }
    }

    [Test]
    public async Task Create_EmptyId_ThrowsArgumentException()
    {
        var action = () => SystemUser.Create(
            new SystemUserId(Guid.Empty),
            "alice",
            "ALICE",
            "argon2-hash",
            "Alice",
            "Ng",
            "alice@example.test",
            null,
            CreatedAt,
            "bootstrap");

        await Assert.That(action).Throws<ArgumentException>();
    }

    [Test]
    public async Task FailedLoginAttempts_SetToNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        var user = CreateUser();
        var action = () => user.FailedLoginAttempts = -1;

        await Assert.That(action).Throws<ArgumentOutOfRangeException>();
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
        CreatedAt,
        "bootstrap");
}
