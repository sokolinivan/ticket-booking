using System.Reflection;
using ArchUnitNET.Domain;
using ArchUnitNET.Loader;

namespace TicketBooking.ArchitectureTests;

/// <summary>
///     Загружает "чистые" backend-сборки модульного монолита одноразово.
///     Здесь намеренно НЕ загружается фикстура-нарушитель
///     (TicketBooking.ArchitectureTests.Fixtures): она используется только
///     в отдельном негативном кейсе (см. NegativeCaseTests).
/// </summary>
internal static class ArchitectureFixture
{
    /// <summary>
    ///     Имена "чистых" backend-сборок. Новые проекты модулей добавляются сюда
    ///     по мере появления, чтобы их правила начинают проверяться автоматически.
    /// </summary>
    private static readonly string[] CleanAssemblyNames =
    [
        "TicketBooking.Companies.Contracts",
        "TicketBooking.Companies.Core",
        "TicketBooking.Api",
        "TicketBooking.BuildingBlocks",
        "TicketBooking.ServiceDefaults",
        "TicketBooking.AppHost",
    ];

    private static readonly Architecture Cached = new ArchLoader()
        .LoadAssemblies(CleanAssemblyNames.Select(Assembly.Load).ToArray())
        .Build();

    /// <summary>Чистая архитектура backend (без фикстур-нарушителей).</summary>
    public static Architecture Load() => Cached;
}
