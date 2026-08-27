# Task 2 Brief — Enable warnings-as-errors and remediate the warning surface

**Plan task:** Task 2 "Enable warnings-as-errors and remediate the warning surface" (from docs/superpowers/plans/2026-08-27-code-analysis-setup.md).

**Language:** Use English (configured Comet artifact language = `en`).

**Goal:** Flip the analyzer/WAE switches in `Directory.Build.props`, append the test-project severity-relaxation section to `.editorconfig`, then run a full solution build and fix every warning so the tree builds warning-clean under warnings-as-errors. Implements OpenSpec tasks.md 2.1 and 2.2.

## Allowed scope
- Modify `Directory.Build.props` (PropertyGroup only).
- Append a `[tests/**/*.cs]` section to the root `.editorconfig`.
- Modify ONLY source/test files needed to clear real build warnings (see below).
- Do NOT check off any plan/OpenSpec task checkboxes.
- Do NOT rename TUnit test methods (underscore names are intentional) and do NOT make `Calculator` static — those are resolved by severity relaxation, not source changes.

## Step 1 — Update `Directory.Build.props`

Replace the entire `<PropertyGroup>` block in `/home/bean/Projects/work/ticket-booking/Directory.Build.props` with:

```xml
<PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AnalysisLevel>latest</AnalysisLevel>
    <AnalysisMode>Recommended</AnalysisMode>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <CodeAnalysisTreatWarningsAsErrors>true</CodeAnalysisTreatWarningsAsErrors>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AllowMissingPrunePackageData>true</AllowMissingPrunePackageData>
</PropertyGroup>
```

Keep all other lines/whitespace in the file as they are.

## Step 2 — Append the `[tests/**/*.cs]` relaxation section to `.editorconfig`

Append to the end of `/home/bean/Projects/work/ticket-booking/.editorconfig`:

```editorconfig

# --- Test projects -----------------------------------------------------------
# TUnit test-method names deliberately use underscores (e.g. Add_ReturnsSum) and
# the test support Calculator is an injectable instance type (ClassDataSource<T>).
# These are intentional conventions, so relax the analyzer rules that would
# otherwise fail the test projects under warnings-as-errors (Design 2.3).
[tests/**/*.cs]
dotnet_diagnostic.CA1707.severity = none
dotnet_diagnostic.CA1822.severity = none
```

## Step 3 — Run the build and fix every warning it produces

Run:
```bash
dotnet build TicketBooking.slnx
```

Under the new settings this will report warnings as errors. Fix ALL of them, applying this policy (from the design):
- **Fix in source** genuine lint issues (e.g. CA1852 sealable types, CA1305 locale-sensitive parsing, CA1816 missing GC.SuppressFinalize).
- **Do NOT fix in source** deliberate test conventions (CA1707 underscore names, CA1822 static-members candidates in the TUnit test-support `Calculator`); these are already relaxed by Step 2's `[tests/**/*.cs]` section.
- If a warning originates from something genuinely unavoidable (an Aspire AppHost SDK/template artifact, generated NuGet tooling, or an unfixable third-party package), isolate it with a per-project `<NoWarn>` plus a one-line comment. Do NOT add a global `<NoWarn>` disabling WAE. Never suppress a warning that is ours to fix (Design 2.2).

Known/predicted warnings to look for (verify against the actual build output; the list may differ):
- `src/Backend/TicketBooking.Api/Program.cs` — CA1852: mark the `WeatherForecast` record `sealed`.
- `tests/TicketBooking.SystemTests/DependencyInjectionTests.cs` — CA1305: `result.ToString()` → `result.ToString(CultureInfo.InvariantCulture)` and add `using System.Globalization;`.
- `tests/TicketBooking.SystemTests/Data/InMemoryDb.cs` — CA1816: add `GC.SuppressFinalize(this);` at the top of `DisposeAsync()`.
- CA1707 / CA1822 in `tests/TicketBooking.SystemTests`: handled by Step 2, do not change source.

NOTE: `src/Backend/TicketBooking.BuildingBlocks/Class1.cs` already ends with a trailing newline and has a UTF-8 BOM; it does NOT need a missing-newline fix — do not touch it.

## Step 4 — Confirm the build is clean

Re-run:
```bash
dotnet build TicketBooking.slnx
```
Expected: `Build succeeded.` with `0 Warning(s)`, `0 Error(s)`, exit code 0.

The commit must include only the minimal source changes needed to reach a warning-clean build.

## Commit

```bash
git add Directory.Build.props .editorconfig <any changed source files>
git commit -m "chore: enable warnings-as-errors and remediate analyzer warnings"
```

(Collect changed files via `git status`; do not stage `docs/openspec/changes/.../.comet/*` or the plan file.)

## Report contract

Return a status `DONE | DONE_WITH_CONCERNS | BLOCKED | NEEDS_CONTEXT` and include: commit hash, changed files (full list), the final build summary (`dotnet build TicketBooking.slnx` → warnings/errors counts and exit code), any `<NoWarn>` added and why, and any concerns. Also report which risk signals from this list apply to the task diff:
- Cross-module / cross-subsystem coordinated change
- Security-sensitive surface (auth, crypto, SQL, secrets)
- Concurrency / locks / shared mutable state
- Data or schema migration
- Public API contract or external interface change
- Single-task diff exceeds 200 lines

Do NOT load or run any Comet/openai skill — implement directly. Do not modify anything outside the allowed scope.
