namespace TicketBooking.ArchitectureTests;

/// <summary>
///     R3. Чистота домена: бизнес-модули не зависят от ASP.NET Core, Aspire,
///     YARP, PaymentEmulator и frontend/React.
/// </summary>
public sealed class DomainPurityTests
{
    [Test]
    public async Task BusinessModules_DoNotDependOnAspire()
    {
        await CheckForbiddenFrameworks();
    }

    [Test]
    public async Task BusinessModules_DoNotDependOnAspNetCore()
    {
        await CheckForbiddenFrameworks();
    }

    [Test]
    public async Task BusinessModules_DoNotDependOnYarp()
    {
        await CheckForbiddenFrameworks();
    }

    [Test]
    public async Task BusinessModules_DoNotDependOnPaymentEmulator()
    {
        await CheckForbiddenFrameworks();
    }

    [Test]
    public async Task BusinessModules_DoNotDependOnFrontendReact()
    {
        await CheckForbiddenFrameworks();
    }

    private static async Task CheckForbiddenFrameworks()
    {
        var architecture = ArchitectureFixture.Load();
        RuleRunner.CheckAll(
            architecture,
            ArchitectureRules.DomainPurityRules(architecture));
        await Assert.Pass();
    }
}
