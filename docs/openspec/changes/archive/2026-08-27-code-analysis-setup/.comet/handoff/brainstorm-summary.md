# Brainstorm Summary

- Change: code-analysis-setup
- Date: 2026-08-27

## Confirmed Technical Approach

1. Root `.editorconfig` with path-scoped sections: general, `[*.cs]`, and `[*.{ts,tsx,js,jsx,json,css,html}]`.
   - C#: 4-space indent, ordering, new-line, filename/namespace handling, `var` prefs, expression-body, accessibility, `this.` off, file-scoped namespaces. IDE-suggestion severity except a small enforced core set.
   - Frontend: 2-space indent, single quotes, semicolons, trailing commas (Prettier/Vite defaults).
2. `Directory.Build.props` shared to all backend + test C# projects:
   - Keep `Nullable=enable`, `ImplicitUsings=enable`, `AnalysisLevel=latest`.
   - Set `AnalysisMode=Recommended` (not All — avoid noise in a young repo).
   - Set `EnforceCodeStyleInBuild=true`, `CodeAnalysisTreatWarningsAsErrors=true`, `TreatWarningsAsErrors=true`.
   - Minimal documented `NoWarn` escape hatch only for unavoidable Aspire/template/third-party warnings.
3. Fix existing warnings so the tree builds warning-clean under WAE; add a negative check showing a deliberate warning fails the build.

## Key Trade-offs and Risks

- Recommended vs All analyzer breadth: Recommended now, escalate later.
- Frontend limited to `.editorconfig` formatting; no eslint wiring (not in Phase 0 scope).
- Turning on WAE may surface existing warnings: fix them; scope a justified NoWarn where unavoidable.

## Testing Strategy

- Full `dotnet build` solution: zero warnings, exit 0.
- Negative test: a deliberately introduced warning makes `dotnet build` fail.
- `dotnet format --verify-no-changes` to confirm formatting conformance.

## Spec Patches

None (change opts out of specs via `skip_specs: true`).
