using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketBooking.Identity;
using TicketBooking.Identity.Internal.Persistence;

namespace TicketBooking.IntegrationTests.Identity;

public class IdentityCompositionTests
{
    [Test]
    public async Task AddIdentityModule_RegistersIdentityDbContextInScope()
    {
        var configuration = new ConfigurationManager
        {
            ["ConnectionStrings:ticketbooking"] =
                "Host=localhost;Port=5432;Database=ticketbooking;Username=postgres;Password=postgres",
        };
        var services = new ServiceCollection();

        services.AddIdentityModule(configuration);
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        await Assert.That(context).IsNotNull();
    }
}
