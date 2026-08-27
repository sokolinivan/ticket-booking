using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace TicketBooking.ArchitectureTests;

/// <summary>
///     Фабрика ArchUnitNET-правил зависимостей модульного монолита.
///     Все правила выражены через namespace-префиксы и вычисляют список
///     бизнес-модулей из переданной архитектуры, поэтому автоматически
///     распространяются на новые модули без правки кода правил.
/// </summary>
internal static class ArchitectureRules
{
    // Состав "запрещённых фреймворков" для бизнес-модулей (чистота домена).
    private static readonly (string Name, string Pattern)[] ForbiddenFrameworks =
    [
        ("ASP.NET Core", @"^Microsoft\.AspNetCore(\..*)?$"),
        ("Aspire", @"^Aspire(\..*)?$"),
        ("YARP", @"^(Yarp|Microsoft\.ReverseProxy)(\..*)?$"),
        ("PaymentEmulator", @"^PaymentEmulator(\..*)?$"),
        ("Frontend/React", @"^(React|Microsoft\.JSInterop)(\..*)?$"),
    ];

    // Composition roots: бизнес-модули не могут на них ссылаться.
    private static readonly string[] CompositionRoots =
        ["Api", "Worker", "DatabaseMigrator", "Gateway"];

    /// <summary>
    ///     R1. Изоляция модулей: типы модуля A не зависят от внутренних слоёв
    ///     (любых, кроме Contracts) любого другого модуля B. Генерируется по одной
    ///     паре (A, B), A ≠ B.
    /// </summary>
    public static IEnumerable<IArchRule> ModuleIsolationRules(Architecture architecture)
    {
        var modules = ModuleRegistry.DiscoverBusinessModules(architecture).ToList();

        foreach (var sourceModule in modules)
        {
            var source = Types().That()
                .ResideInNamespaceMatching(ModuleRegistry.ModuleNamespacePattern(sourceModule))
                .As($"модуль {sourceModule}");

            foreach (var targetModule in modules.Where(m => m != sourceModule))
            {
                var internalTargets = Types().That()
                    .ResideInNamespaceMatching(ModuleRegistry.ModuleNamespacePattern(targetModule))
                    .And()
                    .DoNotResideInNamespaceMatching(
                        ModuleRegistry.ContractsNamespacePattern(targetModule))
                    .As($"внутренние слои модуля {targetModule}");

                yield return source.Should().NotDependOnAny(internalTargets)
                    .Because(
                        $"модуль {sourceModule} не должен зависеть от внутренних слоёв модуля "
                        + $"{targetModule}; разрешён только его публичный Contracts");
            }
        }
    }

    /// <summary>
    ///     R2. Contracts-слой модуля не зависит от внутренних слоёв того же модуля
    ///     (нулевая зависимость вниз: публичные интерфейсы не видят реализацию).
    /// </summary>
    public static IEnumerable<IArchRule> ContractsPurityRules(Architecture architecture)
    {
        foreach (var module in ModuleRegistry.DiscoverBusinessModules(architecture))
        {
            var contracts = Types().That()
                .ResideInNamespaceMatching(ModuleRegistry.ContractsNamespacePattern(module))
                .As($"Contracts модуля {module}");

            var internalLayers = Types().That()
                .ResideInNamespaceMatching(ModuleRegistry.ModuleNamespacePattern(module))
                .And()
                .DoNotResideInNamespaceMatching(
                    ModuleRegistry.ContractsNamespacePattern(module))
                .As($"внутренние слои модуля {module}");

            yield return contracts.Should().NotDependOnAny(internalLayers)
                .Because(
                    $"Contracts модуля {module} не должен зависеть от внутренних слоёв того же модуля");
        }
    }

    /// <summary>
    ///     R3. Чистота домена: бизнес-модули не зависят от ASP.NET Core, Aspire,
    ///     YARP, PaymentEmulator и frontend/React.
    /// </summary>
    public static IEnumerable<IArchRule> DomainPurityRules(Architecture architecture)
    {
        foreach (var module in ModuleRegistry.DiscoverBusinessModules(architecture))
        {
            var source = Types().That()
                .ResideInNamespaceMatching(ModuleRegistry.ModuleNamespacePattern(module))
                .As($"модуль {module}");

            foreach (var (name, pattern) in ForbiddenFrameworks)
            {
                var forbidden = Types().That().ResideInNamespaceMatching(pattern).As(name);

                yield return source.Should().NotDependOnAny(forbidden)
                    .Because($"бизнес-модуль {module} не должен зависеть от {name}");
            }
        }
    }

    /// <summary>
    ///     R4. Направление composition roots: бизнес-модули не зависят от
    ///     Api / Worker / DatabaseMigrator / Gateway.
    /// </summary>
    public static IEnumerable<IArchRule> CompositionRootDirectionRules(Architecture architecture)
    {
        foreach (var module in ModuleRegistry.DiscoverBusinessModules(architecture))
        {
            var source = Types().That()
                .ResideInNamespaceMatching(ModuleRegistry.ModuleNamespacePattern(module))
                .As($"модуль {module}");

            foreach (var root in CompositionRoots)
            {
                var forbidden = Types().That()
                    .ResideInNamespaceMatching("^" + System.Text.RegularExpressions.Regex.Escape(
                        ModuleRegistry.Root + root) + @"(\..*)?$")
                    .As($"composition root {root}");

                yield return source.Should().NotDependOnAny(forbidden)
                    .Because($"бизнес-модуль {module} не должен зависеть от composition root {root}");
            }
        }
    }

    /// <summary>
    ///     R5. Gateway не содержит бизнес-логику: не зависит от внутренних слоёв
    ///     бизнес-модулей (разрешён только их публичный Contracts).
    /// </summary>
    public static IEnumerable<IArchRule> GatewayRules(Architecture architecture)
    {
        var gateway = Types().That()
            .ResideInNamespaceMatching(@"^TicketBooking\.Gateway(\..*)?$")
            .As("Gateway");

        foreach (var module in ModuleRegistry.DiscoverBusinessModules(architecture))
        {
            var internalTargets = Types().That()
                .ResideInNamespaceMatching(ModuleRegistry.ModuleNamespacePattern(module))
                .And()
                .DoNotResideInNamespaceMatching(ModuleRegistry.ContractsNamespacePattern(module))
                .As($"внутренние слои модуля {module}");

            yield return gateway.Should().NotDependOnAny(internalTargets)
                .Because(
                    $"Gateway не должен содержать бизнес-логику и зависеть от внутренних слоёв модуля {module}");
        }
    }
}
