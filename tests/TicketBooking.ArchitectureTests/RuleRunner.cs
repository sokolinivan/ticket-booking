using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;

namespace TicketBooking.ArchitectureTests;

/// <summary>
///     Выполняет набор ArchUnitNET-правил над заданной архитектурой.
/// </summary>
internal static class RuleRunner
{
    /// <summary>Выбрасывает исключение при первом нарушенном правиле.</summary>
    public static void CheckAll(Architecture architecture, IEnumerable<IArchRule> rules)
    {
        foreach (var rule in rules)
        {
            rule.Check(architecture);
        }
    }
}
