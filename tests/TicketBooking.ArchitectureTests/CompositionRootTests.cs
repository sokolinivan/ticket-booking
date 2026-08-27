namespace TicketBooking.ArchitectureTests;

/// <summary>
///     R4. Направление composition roots: бизнес-модули не зависят от
///     Api / Worker / DatabaseMigrator / Gateway.
///     R5. Gateway не содержит бизнес-логики (не зависит от внутренних слоёв модулей).
/// </summary>
public sealed class CompositionRootTests
{
    [Test]
    public async Task BusinessModules_DoNotDependOnCompositionRoots()
    {
        var architecture = ArchitectureFixture.Load();
        RuleRunner.CheckAll(
            architecture,
            ArchitectureRules.CompositionRootDirectionRules(architecture));
        await Assert.Pass();
    }

    [Test]
    public async Task Gateway_DoesNotContainBusinessLogic()
    {
        var architecture = ArchitectureFixture.Load();
        RuleRunner.CheckAll(
            architecture,
            ArchitectureRules.GatewayRules(architecture));
        await Assert.Pass();
    }
}
