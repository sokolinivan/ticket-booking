using Microsoft.EntityFrameworkCore;
using Npgsql;
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
        await ResetAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();

        var builder = new NpgsqlConnectionStringBuilder(_container.GetConnectionString());
        var databaseName = builder.Database ?? throw new InvalidOperationException("The test database name is required.");
        builder.Database = "postgres";
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE {new NpgsqlCommandBuilder().QuoteIdentifier(databaseName)}";
        await command.ExecuteNonQueryAsync();
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
