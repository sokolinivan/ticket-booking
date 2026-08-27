namespace TicketBooking.ArchitectureTests;

/// <summary>
///     R1. Изоляция модулей: ссылки модуля на другие модули допустимы
///     только через их публичный Contracts; внутренние слои чужих модулей
///     недоступны.
/// </summary>
public sealed class ModuleIsolationTests
{
    [Test]
    public async Task BusinessModules_DoNotDependOnOtherModulesInternalLayers()
    {
        var architecture = ArchitectureFixture.Load();
        RuleRunner.CheckAll(
            architecture,
            ArchitectureRules.ModuleIsolationRules(architecture));
        await Assert.Pass();
    }

    [Test]
    public async Task ContractsLayers_DoNotDependOnOwnInternalLayers()
    {
        var architecture = ArchitectureFixture.Load();
        RuleRunner.CheckAll(
            architecture,
            ArchitectureRules.ContractsPurityRules(architecture));
        await Assert.Pass();
    }
}
