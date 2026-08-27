using System.Reflection;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.TUnit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace TicketBooking.ArchitectureTests;

/// <summary>
///     A32. Негативный кейс: доказывает, что правила способны обнаруживать
///     нарушение. Загружает изолированно намеренно "сломанную" фикстуру
///     (TicketBooking.ArchitectureTests.Fixtures), где Sales.Core зависит от
///     внутреннего слоя Customers.Core, и проверяет, что правило изоляции
///     модулей на это срабатывает.
/// </summary>
public sealed class NegativeCaseTests
{
    private static Architecture LoadFixtureArchitecture()
        => new ArchLoader().LoadAssemblies(Assembly.Load("TicketBooking.ArchitectureTests.Fixtures"))
            .Build();

    [Test]
    public async Task ModuleIsolation_Rule_DetectsCrossModuleInternalDependency()
    {
        var architecture = LoadFixtureArchitecture();
        var rules = ArchitectureRules.ModuleIsolationRules(architecture).ToList();

        var failing = new List<IArchRule>();
        foreach (var rule in rules)
        {
            try
            {
                rule.Check(architecture);
            }
            catch (FailedArchRuleException)
            {
                failing.Add(rule);
            }
        }

        // Фикстура содержит зависимость Sales.Core -> Customers.Core (внутренний слой),
        // поэтому по крайней мере одно правило изоляции должно сработать.
        await Assert.That(failing).IsNotEmpty()
            .Because("правило изоляции модулей должно ловить зависимость Sales.Core -> Customers.Core");
    }

    [Test]
    public async Task ModuleIsolation_ReverseDirection_DoesNotTrigger()
    {
        // Sales.Core зависит от Customers.Core, но Customers.Core не зависит от Sales.Core:
        // правило (Customers -> Sales) не должно срабатывать (точность правила).
        var architecture = LoadFixtureArchitecture();
        var rules = ArchitectureRules.ModuleIsolationRules(architecture).ToList();

        var failing = new List<IArchRule>();
        foreach (var rule in rules)
        {
            try
            {
                rule.Check(architecture);
            }
            catch (FailedArchRuleException)
            {
                failing.Add(rule);
            }
        }

        var customerToSales = failing.Any(r =>
            r.Description.Contains("модуль Customers", StringComparison.Ordinal) &&
            r.Description.Contains("модуль Sales", StringComparison.Ordinal));

        await Assert.That(customerToSales).IsFalse()
            .Because("Customers.Core не зависит от Sales.Core, поэтому правило Customers -> Sales не должно срабатывать");
    }
}
