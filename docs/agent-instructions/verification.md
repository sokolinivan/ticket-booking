# Verification and Testing

Use this file when validating changes or adding tests.

## Full Static Verification

After restoring dependencies, run the checks relevant to the changed areas. The complete local set is:

```bash
dotnet build TicketBooking.slnx --no-restore
dotnet format TicketBooking.slnx --verify-no-changes --no-restore
pnpm --dir src/Frontend/public-web lint
pnpm --dir src/Frontend/public-web build
pnpm --dir src/Frontend/backoffice-web lint
pnpm --dir src/Frontend/backoffice-web build
```

Each frontend `build` runs `tsc -b` before the Vite production build; there is no separate typecheck command. Frontend build artifacts are under each application's `dist/`; .NET artifacts are under `bin/` and `obj/`. Do not commit them.

For formatting-only changes, run `dotnet format TicketBooking.slnx --verify-no-changes --no-restore` and lint both frontends before completion.

Warnings, analyzers, and code-style diagnostics fail the .NET build intentionally. If a build fails on a warning, inspect `Directory.Build.props` and the corresponding `.editorconfig` rule rather than suppressing it globally.

## Test Commands

Tests use TUnit. Currently, executable tests exist only in `tests/TicketBooking.SystemTests`; Unit, Integration, and Architecture projects are empty scaffolds.

Run all current tests after building:

```bash
dotnet run --project tests/TicketBooking.SystemTests --no-build
```

List discovered tests:

```bash
dotnet run --project tests/TicketBooking.SystemTests --no-build -- --list-tests --no-ansi
```

Run one test by name:

```bash
dotnet run --project tests/TicketBooking.SystemTests --no-build -- --treenode-filter "/*/*/*/Add_ReturnsSum" --minimum-expected-tests 1
```

For a class, use a filter such as `"/*/*/BasicTests/*"`. Always include `--minimum-expected-tests 1` with a narrow filter so a typo cannot succeed with zero tests. If TUnit reports `Zero tests ran`, fix the four-part tree-node path rather than removing that safeguard.

Do not use `dotnet test TicketBooking.slnx`. The current Microsoft Testing Platform configuration on .NET 10 does not enable the new `dotnet test` mode and fails at the VSTest target. Run the TUnit executable through `dotnet run` until the solution configuration changes explicitly.

## Test Rules

- Add or update tests for new behavior and regressions even though no coverage threshold is configured.
- Name tests `Method_Scenario_Result`; `.editorconfig` intentionally permits underscores in `tests/**/*.cs`.
- Put pure domain-rule tests in `TicketBooking.UnitTests`.
- Put tests using real infrastructure or PostgreSQL in `TicketBooking.IntegrationTests`.
- Put module-boundary checks in `TicketBooking.ArchitectureTests`.
- Put end-to-end API or UI scenarios in `TicketBooking.SystemTests`.
- Do not label tests that use only in-memory substitutes as integration tests.

In the final report, list commands actually run and state explicitly when a check was skipped or unavailable.
