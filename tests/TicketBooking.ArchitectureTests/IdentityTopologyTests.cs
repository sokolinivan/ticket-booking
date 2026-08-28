namespace TicketBooking.ArchitectureTests;

using System.Text.RegularExpressions;

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
        var api = GetYamlBlock(compose, "ticketbooking-api", 2);
        var postgres = GetYamlBlock(compose, "postgres", 2);

        await Assert.That(Regex.IsMatch(
            postgres,
            @"(?m)^\s*-\s*postgres-data:/var/lib/postgresql\s*$")).IsTrue();
        await Assert.That(GetYamlBlock(compose, "volumes", 0)).Contains("postgres-data:");
        await Assert.That(GetYamlBlock(postgres, "healthcheck", 4)).Contains("pg_isready");
        await Assert.That(GetYamlBlock(api, "postgres", 6)).Contains("condition: service_healthy");

        var connectionString = Regex.Match(
            api,
            @"(?m)^\s+ConnectionStrings__ticketbooking:\s*[""']?(?<value>[^\r\n""']+)");

        await Assert.That(connectionString.Success).IsTrue();
        await Assert.That(connectionString.Groups["value"].Value).Contains("Host=postgres;");
        await Assert.That(connectionString.Groups["value"].Value).Contains("Database=${POSTGRES_DB:-ticketbooking};");
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

        var credentialValues = Regex.Matches(
                configuration,
                @"(?im)(?:POSTGRES_PASSWORD|Password[""']?)\s*[:=]\s*[""']?(?<value>[^;\r\n""']+)")
            .Select(match => match.Groups["value"].Value);

        await Assert.That(credentialValues).IsNotEmpty();
        await Assert.That(credentialValues).All(value => value.StartsWith("${", StringComparison.Ordinal));
        await Assert.That(configuration).DoesNotMatch(@"(?i)postgres(?:ql)?://[^\s:/]+:[^\s@]+@");
    }

    private static string GetYamlBlock(string yaml, string key, int indentation)
    {
        var lines = yaml.Split('\n');
        var start = Array.FindIndex(
            lines,
            line => line == $"{new string(' ', indentation)}{key}:" ||
                    line.StartsWith($"{new string(' ', indentation)}{key}: ", StringComparison.Ordinal));

        if (start < 0)
        {
            return string.Empty;
        }

        return string.Join(
            '\n',
            lines.Skip(start + 1).TakeWhile(line =>
                string.IsNullOrWhiteSpace(line) || line.TakeWhile(char.IsWhiteSpace).Count() > indentation));
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
