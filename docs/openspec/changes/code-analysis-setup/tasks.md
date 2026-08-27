## 1. Unified formatting rules

- [x] 1.1 Create a repository-root `.editorconfig` with general, C#, and frontend (TypeScript/JavaScript) scoped sections and verify it is picked up by `dotnet format` (no C# files are listed as non-conforming)
- [x] 1.2 Define C# style rules (indentation, blank lines, naming, implicit/explicit types, `var` preference, accessibility modifiers, expression-body, `this.` qualifier, new-line, Unicode/UTF-8 header) consistent with the existing repo style and verify a sample file reformats without changes
- [x] 1.3 Define frontend TypeScript/JavaScript rules aligned with the existing Vite/React projects (2-space indent, quotes, trailing commas, semicolons) and verify they do not conflict with C# rules (path-scoped sections)

## 2. Warnings as errors

- [ ] 2.1 Update `Directory.Build.props` to enable analyzer enforcement: `AnalysisMode=Recommended` (or `All`), `EnforceCodeStyleInBuild=true`, `CodeAnalysisTreatWarningsAsErrors=true`, `TreatWarningsAsErrors=true`, and keep `Nullable=enable` and `AnalysisLevel=latest`
- [ ] 2.2 Run a full `dotnet build` over the solution and fix every compiler/analyzer warning the current tree produces (adding only justified per-warning `NoWarn` where the warning comes from unavoidable Aspire/template or third-party code) so the tree builds with zero warnings
- [ ] 2.3 Confirm a deliberately introduced warning (e.g., a trivial unused-field or style violation) causes `dotnet build` to fail, then remove it, proving warnings-as-errors is active

## 3. Verification

- [ ] 3.1 Run `dotnet format --verify-no-changes` across the solution and verify it reports no changes needed
- [ ] 3.2 Rebuild the full solution cleanly with warnings-as-errors active and verify exit code 0 with zero warnings
