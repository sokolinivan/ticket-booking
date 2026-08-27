using System.Text.RegularExpressions;
using ArchUnitNET.Domain;

namespace TicketBooking.ArchitectureTests;

/// <summary>
///     Помогает определять бизнес-модули и их слои по namespace-префиксам.
///     Соглашение: namespace бизнес-модуля имеет вид
///     <c>TicketBooking.&lt;Module&gt;.&lt;Layer&gt;</c>, где Module может быть составным
///     (например <c>Identity.Customers</c>), а Layer — один из recognised слоёв
///     (Contracts, Core, Domain, Application, Infrastructure). Модуль идентифицируется
///     префиксом <c>TicketBooking.&lt;Module&gt;.</c>.
/// </summary>
internal static class ModuleRegistry
{
    public const string Root = "TicketBooking.";

    private static readonly string[] Layers =
        ["Contracts", "Core", "Domain", "Application", "Infrastructure"];

    /// <summary>Служебные проекты, которые не являются бизнес-модулями.</summary>
    private static readonly HashSet<string> ServiceModules = new(StringComparer.Ordinal)
    {
        "Api",
        "Worker",
        "DatabaseMigrator",
        "Gateway",
        "ServiceDefaults",
        "BuildingBlocks",
        "AppHost",
    };

    private sealed record ModuleInfo(string Name, string Layer);

    /// <summary>
    ///     Разбирает namespace бизнес-модуля. Возвращает null, если namespace
    ///     не принадлежит бизнес-модулю (служебный проект, отсутствие слоя и т.п.).
    /// </summary>
    private static ModuleInfo? Parse(string ns)
    {
        if (!ns.StartsWith(Root, StringComparison.Ordinal))
        {
            return null;
        }

        string rest = ns[Root.Length..];
        var segments = rest.Split('.');
        if (segments.Length < 2)
        {
            return null;
        }

        string layer = segments[^1];
        if (!Layers.Contains(layer) || segments.Length < 2)
        {
            return null;
        }

        string module = string.Join(".", segments[..^1]);
        if (module.Length == 0 || ServiceModules.Contains(module))
        {
            return null;
        }

        return new ModuleInfo(module, layer);
    }

    /// <summary>Namespace префикс модуля: <c>TicketBooking.&lt;Module&gt;.</c></summary>
    public static string ModulePrefix(string module) => Root + module + ".";

    /// <summary>Regex, матчащий любой namespace, принадлежащий модулю (включая вложенные).</summary>
    public static string ModuleNamespacePattern(string module)
        => "^" + Regex.Escape(ModulePrefix(module)) + ".*";

    /// <summary>Regex, матчащий Contracts-слой модуля (и его под-namespace).</summary>
    public static string ContractsNamespacePattern(string module)
        => "^" + Regex.Escape(ModulePrefix(module) + "Contracts") + "(\\..*)?$";

    /// <summary>Имя последнего слоя в namespace, либо null, если это не модульный namespace.</summary>
    public static string? Layer(string ns) => Parse(ns)?.Layer;

    /// <summary>Имя модуля, которому принадлежит namespace, либо null.</summary>
    public static string? Module(string ns) => Parse(ns)?.Name;

    /// <summary>Является ли namespace Contracts-слоем какого-либо модуля.</summary>
    public static bool IsContractsNamespace(string ns) => Layer(ns) == "Contracts";

    /// <summary>
    ///     Обнаруживает бизнес-модули, реально присутствующие в загруженной архитектуре,
    ///     чтобы правила автоматически распространялись на новые модули.
    /// </summary>
    public static IEnumerable<string> DiscoverBusinessModules(Architecture architecture)
        => architecture.Types
            .Select(t => t.Namespace?.Name ?? string.Empty)
            .Select(Module)
            .Where(m => m is not null)
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .OrderBy(m => m, StringComparer.Ordinal);
}
