## Context

See `proposal.md` - Why. Current state: there is no `.editorconfig`; `Directory.Build.props` already sets `TargetFramework=net10.0`, `ImplicitUsings`, `Nullable=enable`, `AnalysisLevel=latest`, `AnalysisMode=None`, `TreatWarningsAsErrors=false`, `CodeAnalysisTreatWarningsAsErrors=false`, `EnforceCodeStyleInBuild=false`. Backend projects inherit these shared settings; test projects also live under `tests/` and inherit the same `Directory.Build.props`.

## Goals / Non-Goals

**Goals:**
- Enforce a single repository-wide formatting and style contract via `.editorconfig`.
- Make compiler and analyzer warnings fail the backend build to keep code quality high.
- Keep the setup automatic: changes in `Directory.Build.props` and a root `.editorconfig` require no per-project edits.
- Preserve NRT (`Nullable=enable`) as the baseline; plan.md already notes NRT is on.

**Non-Goals:**
- No architecture/dependency rules (separate Phase 0 task: architecture tests).
- No GitHub Actions workflow (separate Phase 0 task).
- No analyzer package additions unless strictly required; prefer the built-in analyzers.
- No behavior or API changes; this is tooling only.

## Decisions

**1. Single root `.editorconfig`.** Place one `.editorconfig` at the repository root.
- Why: unified rules apply to all C# and frontend sources, and are honored by Rider/VS Code/Visual Studio and by `dotnet format`. Rollup of severity settings centralizes policy.
- Alternatives: per-project editorconfig files — rejected (fragmented, drift-prone), separate C# vs frontend files — rejected (root file can scope per-extension and per-path via sections).

**2. Enforce C# code-style diagnostics through `AnalysisMode` + `EnforceCodeStyleInBuild` in `Directory.Build.props`, not only via editorconfig severity.**
- Why: `TreatWarningsAsErrors` upgrades compiler warnings to errors, but code-style/analyzer diagnostics only become build errors when `CodeAnalysisTreatWarningsAsErrors=true` and, for style rules, `EnforceCodeStyleInBuild=true`. Setting these in the shared props guarantees enforcement on the CI build, independent of editor configuration.
- Alternatives: rely on `.editorconfig` severity only — rejected (does not fail a `dotnet build` without `EnforceCodeStyleInBuild`).

**3. Warnings as errors in `Directory.Build.props` (System-wide), with a documented escape hatch.** Enable `TreatWarningsAsErrors=true` and `CodeAnalysisTreatWarningsAsErrors=true` for all projects, and fix any pre-existing warnings so the tree compiles cleanly.
- Why: uniform, cannot be forgotten per project; plan.md explicitly wants WAE for backend projects.
- Alternatives: scope WAE to `src/Backend/**` only via a nested `Directory.Build.props` — rejected because the plan's cross-cutting setup and CI both benefit from solution-level enforcement; test projects should also be warning-clean.
- Escape hatch: where a specific warning is genuinely unavoidable or a third-party analyzer is noisy, add a targeted `<NoWarn>` (documented in tasks) rather than weakening the global switch.

**4. Nullability: keep `Nullable=enable` at solution level.** No change needed; it is already on. NRT-related warnings (nullable reference type warnings) become errors like everything else under WAE.

## Risks / Trade-offs

- **Existing warnings break the build once WAE is on.** → Land `.editorconfig` and WAE together and run a full `dotnet build`; fix all warnings produced by the current tree before closing the phase. Where a warning is from an unavoidable external/Aspire template detail, scope a minimal `NoWarn` with justification.
- **Over-strict style rules create friction / churn.** → Prefer IDE-default or common style rules and keep the initial rule set conservative; the editorconfig can be tightened later.
- **`.editorconfig` across both C# and TS/JS can conflict in editors.** → Use extension/path-scoped sections so C# rules and frontend rules do not interfere.
- **CI not yet wired (separate task).** → WAE is verified by local `dotnet build`; the GitHub Actions task (Phase 0) will run the same build so CI inherits enforcement automatically.

## Migration Plan

1. Add root `.editorconfig` with general, C#, and frontend sections.
2. Update `Directory.Build.props` to enforce analyzers and warnings as errors.
3. Run `dotnet build` on the full solution; fix resulting warnings or add justified `NoWarn`.
4. Verify with `dotnet format --verify-no-changes` that sources conform to `.editorconfig`.

## Open Questions

None. Any decision that could change the rule set or WAE scope is resolved above; remaining choices (specific style knobs) are refinement, safe to tune during build without changing tasks/approach.
