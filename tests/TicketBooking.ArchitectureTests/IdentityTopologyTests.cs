namespace TicketBooking.ArchitectureTests;

public class IdentityTopologyTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Test]
    public async Task AppHost_IdentityDatabase_ReferencesAndWaitsForSharedPostgres()
    {
        var appHost = await File.ReadAllTextAsync(Path.Combine(
            RepositoryRoot,
            "src",
            "Aspire",
            "TicketBooking.AppHost",
            "AppHost.cs"));

        await Assert.That(appHost).Contains("AddPostgres(\"postgres\")");
        await Assert.That(appHost).Contains("AddDatabase(\"ticketbooking\")");
        await Assert.That(appHost).Contains("WithReference(database)");
        await Assert.That(appHost).Contains("WaitFor(database)");
    }

    [Test]
    public async Task Compose_IdentityDatabase_IsPersistentHealthyAndRequiredByApi()
    {
        var compose = await File.ReadAllTextAsync(Path.Combine(RepositoryRoot, "compose.yaml"));

        await Assert.That(compose).Contains("postgres:");
        await Assert.That(compose).Contains("postgres-data:");
        await Assert.That(compose).Contains("pg_isready");
        await Assert.That(compose).Contains("condition: service_healthy");
        await Assert.That(compose).Contains("ConnectionStrings__ticketbooking:");
    }

    [Test]
    public async Task TopologyConfiguration_DoesNotContainCommittedCredentials()
    {
        var paths = new[]
        {
            Path.Combine(RepositoryRoot, "compose.yaml"),
            Path.Combine(RepositoryRoot, "src", "Aspire", "TicketBooking.AppHost", "AppHost.cs"),
            Path.Combine(RepositoryRoot, "src", "Aspire", "TicketBooking.AppHost", "appsettings.json"),
            Path.Combine(RepositoryRoot, "src", "Aspire", "TicketBooking.AppHost", "appsettings.Development.json")
        };

        var configuration = string.Join(
            '\n',
            await Task.WhenAll(paths.Where(File.Exists).Select(path => File.ReadAllTextAsync(path))));

        await Assert.That(configuration).DoesNotContain("Password=test");
        await Assert.That(configuration).DoesNotContain("Password=postgres");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Environment.CurrentDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TicketBooking.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root.");
    }
}
