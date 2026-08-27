using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.TUnit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using TicketBooking.Identity;

namespace TicketBooking.ArchitectureTests;

public class IdentityModuleArchitectureTests
{
    private static readonly Architecture _architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(DependencyInjection).Assembly,
            System.Reflection.Assembly.Load("TicketBooking.Companies.Core"),
            System.Reflection.Assembly.Load("TicketBooking.Companies.Contracts"),
            System.Reflection.Assembly.Load("TicketBooking.BuildingBlocks"),
            System.Reflection.Assembly.Load("TicketBooking.Api"))
        .Build();

    [Test]
    public async Task NonIdentityModules_DependingOnIdentityPersistence_IsForbidden()
    {
        Types().That().DoNotResideInAssembly(typeof(DependencyInjection).Assembly)
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(
                "^TicketBooking\\.Identity\\.Internal\\.Persistence(\\..*)?$"))
            .Check(_architecture);

        await Task.CompletedTask;
    }

    [Test]
    public async Task IdentityPersistenceTypes_PublicVisibility_IsForbidden()
    {
        Types().That().ResideInNamespaceMatching(
                "^TicketBooking\\.Identity\\.Internal\\.Persistence(\\..*)?$")
            .Should().NotBePublic().WithoutRequiringPositiveResults().Check(_architecture);

        await Task.CompletedTask;
    }
}
