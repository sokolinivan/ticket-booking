# Task 8 Report

## RED

- Added `IdentityMigrationMetadataTests` before generating a migration.
- Initial execution exposed an EF Relational 10.0.4/10.0.11 assembly conflict. Adding a direct centrally-versioned `Microsoft.EntityFrameworkCore.Relational` reference made the test executable.
- Re-ran the scoped test and observed the intended failure: expected one migration, discovered zero.

Command:

```bash
dotnet run --project tests/TicketBooking.IntegrationTests -- --treenode-filter "/*/*/IdentityMigrationMetadataTests/*" --minimum-expected-tests 1
```

Expected RED result:

```text
Expected to have count equal to 1
but found 0
```

## GREEN

- Added a design-time factory using a non-secret placeholder connection and the runtime migrations assembly/history settings.
- Generated `InitialIdentity`, its designer, and the model snapshot with EF tooling.
- Inspected `Up`, `Down`, designer, and snapshot. The migration creates exactly five tables in `identity`, uses four schema-internal foreign keys, includes required indexes and `Version bigint` columns, and has `PasswordHash` but no plaintext `Password` column. `Down` drops only the five Identity tables.
- Added a file-local `CA1861` suppression because EF generated composite-index array arguments that fail this repository's warnings-as-errors build. No generated operation was changed.

Commands:

```bash
dotnet tool restore
dotnet ef migrations add InitialIdentity --project src/Backend/Modules/TicketBooking.Identity.Core --context IdentityDbContext --output-dir Internal/Persistence/Migrations
dotnet build tests/TicketBooking.IntegrationTests/TicketBooking.IntegrationTests.csproj
dotnet run --project tests/TicketBooking.IntegrationTests --no-build -- --treenode-filter "/*/*/IdentityModelMetadataTests/*" --minimum-expected-tests 1
dotnet run --project tests/TicketBooking.IntegrationTests --no-build -- --treenode-filter "/*/*/IdentityMigrationMetadataTests/*" --minimum-expected-tests 1
```

Results:

- Build: succeeded with 0 warnings and 0 errors.
- Identity model metadata: 3 passed, 0 failed.
- Identity migration metadata: 1 passed, 0 failed.

## Risks

- This repository has no local .NET tool manifest, so `dotnet tool restore` reported that no manifest was found. Migration generation then succeeded with the installed `dotnet-ef` 10.0.11 tool.
- Npgsql 10.0.3 resolves EF Relational 10.0.4 unless the centrally managed 10.0.11 Relational package is referenced directly. The explicit reference prevents the runtime assembly conflict and keeps EF runtime/design/tooling versions aligned.
- Database application against a live PostgreSQL instance is outside Task 8; verification is metadata-based as required by the task brief.
