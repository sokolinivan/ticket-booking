## Why

Phase 0 of the TicketBooking MVP plan requires unified code formatting and a fast feedback loop on compiler quality. Today there is no `.editorconfig`, so formatting rules are not enforced consistently across the repo and across IDEs. Warnings are enabled (`AnalysisLevel=latest`) but are not treated as errors, so a compilation warning does not fail the build and can silently degrade code quality.

## What Changes

- Add a repository-wide `.editorconfig` defining unified formatting, naming, and static-analysis style rules for C# and the frontend TypeScript/JavaScript sources.
- Enable **warnings as errors** for all backend C# projects so that compiler and analyzer warnings fail the build.
- Keep nullability (`Nullable=enable` at the solution level) as the baseline and fold NRT into the stricter build settings.
- Share the non-negotiable compiler settings through `Directory.Build.props` so every backend project picks them up automatically.

This is engineering tooling only: no application behavior or API changes.

## Capabilities

This change is pure tooling and build configuration. No spec-level behavior changes, so it opts out of specs via `skip_specs: true` in `.openspec.yaml`.

## Impact

- **New**: `.editorconfig` at the repository root governing C# and frontend formatting.
- **Modified**: `Directory.Build.props` to enable `TreatWarningsAsErrors`, analyzer/config settings, and any build adjustments needed for existing warnings to compile cleanly.
- **Affected projects**: all backend C# projects (`src/Aspire/**`, `src/Backend/**`, `tests/**`) via the shared props; frontend sources via `.editorconfig` only.
- **Tooling**: Rider/VS Code/Visual Studio formatting and analysis settings for C# and TypeScript.
