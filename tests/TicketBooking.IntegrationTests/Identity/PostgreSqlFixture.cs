using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TicketBooking.Identity.Internal.Persistence;
using TUnit.Core.Interfaces;

namespace TicketBooking.IntegrationTests.Identity;

public sealed class PostgreSqlFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.1-alpine")
        .WithDatabase("ticketbooking_identity_tests")
        .WithUsername("ticketbooking_tests")
        .WithPassword("ticketbooking_tests_password")
        .Build();

    public Task InitializeAsync() => _container.StartAsync();

    public async Task ResetAndMigrateAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    internal IdentityDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(_container.GetConnectionString(), npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "identity"))
            .Options;

        return new IdentityDbContext(options);
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
