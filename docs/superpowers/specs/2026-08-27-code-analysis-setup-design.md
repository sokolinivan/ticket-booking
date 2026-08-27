---
comet_change: code-analysis-setup
role: technical-design
canonical_spec: openspec
---

# Design Doc: Code Analysis & Formatting Setup

Deep technical design for implementing the two Phase 0 items from `docs/plan.md`: unified `.editorconfig` formatting rules and warnings as errors for backend C# projects. This deepens the open-phase `design.md`; it does not replace it. See `proposal.md` - Why and `design.md` - Decisions for motivation.

## 1. Environment

- Backend: .NET 10, SDK-style projects, shared `Directory.Build.props` + `Directory.Packages.props` (Central Package Management).
- Projects: `src/Aspire/TicketBooking.AppHost`, `src/Aspire/TicketBooking.ServiceDefaults`, `src/Backend/*` (Api, BuildingBlocks, Modules/*), and `tests/*` (UnitTests, IntegrationTests, ArchitectureTests, SystemTests).
- Frontend: two `.esproj` Vite + React + TypeScript apps under `src/Frontend/{public-web,backoffice-web}`. `client-web.esproj` already has `node_modules`/`pnpm-lock.yaml`.
- Current state: no `.editorconfig`; `Directory.Build.props` has `AnalysisMode=None`, `TreatWarningsAsErrors=false`, `CodeAnalysisTreatWarningsAsErrors=false`, `EnforceCodeStyleInBuild=false`, `Nullable=enable`, `AnalysisLevel=latest`.

## 2. Design

### 2.1 Root `.editorconfig`

Single file at repository root. Structure:

```
root = true

[*]            # general defaults: charset utf-8, end_of_line, insert_final_newline, trim_trailing_whitespace, indent_style

[*.cs]         # C# rules: 4-space indent, usings ordering, blank lines, new-line chars,
               #   expression-body, accessibility modifiers, var prefs, 'this.' off,
               #   file-scoped namespaces, Unicode scalar-values, filename/type matching.
               #   Analyzer defaults via dotnet_style_* / dotnet_diagnostic.* entries.
               #   style rules: severity=warning only for the small enforced core; rest=suggestion.

[*.{ts,tsx,js,jsx}]   # 2-space indent, single quotes, semicolons, trailing commas (Prettier/Vite defaults)
[*.{json,jsonc}]      # 2-space indent for config files
[*.{css,scss,html,xml,csproj,slnx}]  # 2-space or convention defaults
```

Description of rule selection:
- Use `dotnet` analyzers' canonical `.editorconfig` knob names (`csharp_*`, `dotnet_style_*`).
- Keep the initial rule set conservative; most rules `suggestion` so the editor guides without failing the build. Only a deliberately chosen core set is `warning`/`error` to keep formatting enforcement while avoiding churn in a repository that is still actively scaffolded.
- Frontend rules mirror Prettier defaults used by the Vite templates (2-space, single quotes, semicolons, es5 trailing commas) so `.editorconfig` and Prettier agree.

### 2.2 `Directory.Build.props` — warnings as errors

Targeted change to the existing file (`src/` and `tests/` both inherit it):

```xml
<PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AnalysisLevel>latest</AnalysisLevel>
    <AnalysisMode>Recommended</AnalysisMode>          <!-- was None -->
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>  <!-- was false -->
    <CodeAnalysisTreatWarningsAsErrors>true</CodeAnalysisTreatWarningsAsErrors> <!-- was false -->
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>   <!-- was false -->
    <AllowMissingPrunePackageData>true</AllowMissingPrunePackageData>
</PropertyGroup>
```

Rationale (deepens open-phase `design.md`):
- **`AnalysisMode=Recommended`** enables the default .NET analyzer breadth. Chosen over `All` because `All` enables every documented rule and produces heavy churn in a repo that is still being scaffolded; can be escalated to `All` later via a single property.
- **`EnforceCodeStyleInBuild=true`** makes editor-style diagnostics (`IDE*`) apply during build.
- **`CodeAnalysisTreatWarningsAsErrors=true`** turns analyzer warnings into errors; **`TreatWarningsAsErrors=true`** does the same for compiler warnings.
- NRT (`Nullable=enable`) unchanged; nullable warnings become errors under WAE like everything else.
- **Escape hatch**: if the clean build surfaces a warning that is genuinely unavoidable (an Aspire AppHost SDK/template artifact, generated NuGet tooling, or a third-party package we cannot fix), suppress that specific warning via `<NoWarn>` scoped to the affected project (or a justified global `<NoWarn>` if truly global), accompanied by a one-line comment explaining why. Deliberate warnings in our own code must be fixed, never suppressed.

### 2.3 Existing-warning remediation

Run `dotnet build` on the full solution after enabling WAE, collect all warnings, and fix each in source. Where a warning is a style/IDE rule we chose not to enforce as an error, adjust severity in `.editorconfig` rather than suppressing the diagnostic. Document any `NoWarn` entries.

### 2.4 Frontend formatting

Scope is `.editorconfig` formatting rules only. No ESLint/Prettier CLI wiring: the plan's Phase 0 does not require a frontend lint pipeline, and the Vite templates do not ship one by default. The `.editorconfig` section is written so it is compatible with Prettier defaults, so a future Prettier/ESLint rollout aligns out of the box.

## 3. Testing Strategy

| Verification | Command | Pass criterion |
| --- | --- | --- |
| Warnings as errors active | `dotnet build` over the solution | exit 0, zero warnings |
| Negative proof | temporarily introduce a trivial warning (e.g. unused private field or an IDE-style violation), `dotnet build` must fail; remove it | build fails with the warning-as-error, then returns to exit 0 |
| Formatting conforms | `dotnet format --verify-no-changes` | reports no files need formatting |
| Analyzer breadth sane | review build output | Recommended-mode rules cause no unexpected failures |

## 4. Edge Cases and Boundary Conditions

- **Test projects**: they inherit `Directory.Build.props` too; keep WAE uniform so tests are also warning-clean (counts toward CI quality).
- **Aspire AppHost** uses a custom SDK (`Aspire.AppHost.Sdk`) and may emit warnings from the SDK/template; expect to isolate any such warning with a justified `NoWarn` rather than disabling WAE globally.
- **`.editorconfig` cross-language conflicts**: path-scoped `[*.cs]` and `[*.{ts,tsx,...}]` sections prevent C# and frontend rules from interfering; a single root file is honored by Rider/VS/VS Code and `dotnet format`.
- **Generated/`obj` files**: excluded from formatting expectations; `dotnet format` and analyzers ignore generated code.

## 5. Risks and Mitigations

- **WAE reveals many pre-existing warnings** → remediate in the same task loop; scope a small, documented `NoWarn` only where unavoidable.
- **Over-strict style rules cause commit churn** → initial `.editorconfig` keeps most rules at `suggestion`; escalate enforced rules only deliberately.
- **Frontend formatting not enforced in build** → accepted; out of Phase 0 scope and captured as a future lint task.

## 6. Open Questions

None that would change the spec, approach, or task breakdown. Remaining variables (exact rule severities, which specific `NoWarn` entries are needed) are refinement and are resolved empirically during Build by the clean-build + negative-test loop.
