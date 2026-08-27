---
change: code-analysis-setup
design-doc: docs/superpowers/specs/2026-08-27-code-analysis-setup-design.md
base-ref: 0d576b1d4edaf0986065f02811752d53a54862f1
---

# Code Analysis & Formatting Setup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Introduce a root `.editorconfig`, enable warnings-as-errors for the .NET tree, remediate all surfaced warnings, and prove the setup with a warning-clean build, a negative proof-of-failure, and `dotnet format --verify-no-changes`.

**Architecture:** A single repository-root `.editorconfig` (path-scoped `[*.cs]` and `[*.{ts,tsx,...}]` sections so C# and frontend rules do not conflict) plus a targeted edit to `Directory.Build.props` that flips `AnalysisMode=Recommended`, `EnforceCodeStyleInBuild=true`, `CodeAnalysisTreatWarningsAsErrors=true`, and `TreatWarningsAsErrors=true`. The analyzer/formatting surface is then driven to zero warnings: genuine lint issues are fixed in source, deliberate test-convention rules are relaxed in the `.editorconfig` (scoped `[tests/**/*.cs]` section), and unavoidable third-party/Aspire warnings get a justified `NoWarn`.

**Tech Stack:** .NET 10 SDK-style projects with shared `Directory.Build.props` and Central Package Management (`Directory.Packages.props`); root solution `TicketBooking.slnx`; two Vite + React + TypeScript `.esproj` apps under `src/Frontend/{public-web,backoffice-web}`; root `.editorconfig` honored by Rider/VS/VS Code and `dotnet format`.

**Spec:** [Design Doc](docs/superpowers/specs/2026-08-27-code-analysis-setup-design.md) — the plan argues from it; executors must read both the design doc and this plan. Task boundaries from `docs/openspec/changes/code-analysis-setup/tasks.md`.

## Global Constraints

- Single root `.editorconfig` at repository root: `root = true`, then general `[*]`, `[*.cs]`, `[*.{ts,tsx,js,jsx}]`, `[*.{json,jsonc}]`, `[*.{css,scss,html,xml,csproj,slnx}]` sections (Design 2.1).
- Use canonical analyzer/formatting knob names (`csharp_*`, `dotnet_style_*`, `dotnet_diagnostic.*`, `dotnet_sort_*`).
- Keep the initial enforced rule set conservative; only a deliberately chosen core set is `warning`/`error`; the rest stay `suggestion`.
- `Directory.Build.props` must keep `TargetFramework=net10.0`, `ImplicitUsings=enable`, `Nullable=enable` (NRT unchanged — nullable warnings become errors under WAE like everything else), `AnalysisLevel=latest`, `AllowMissingPrunePackageData=true` (Design 2.2).
- Escape hatch: a clean build failure caused by an unavoidable warning (Aspire AppHost SDK/template artifact, generated NuGet tooling, or an unfixable third-party package) is handled via a per-project (or justified global) `<NoWarn>` plus a one-line comment. Deliberate warnings in our own code must be fixed in source, never suppressed (Design 2.2, 2.3).
- A warning from a style/IDE rule we choose not to enforce as an error is handled by adjusting its severity in the `.editorconfig` — not by `<NoWarn>` (Design 2.3).
- Frontend scope is `.editorconfig` formatting rules only. No ESLint/Prettier CLI wiring; the TS/JS section must agree with Prettier/Vite defaults (2-space, single quotes, semicolons, es5 trailing commas) for a future rollout (Design 2.4).
- Build and verification always run against the root solution: `dotnet build` on `TicketBooking.slnx`. `dotnet format` also operates on `TicketBooking.slnx`.
- Warnings-as-errors applies uniformly, including test projects and the AppHost (Design 4).

---

### Task 1: Create the root `.editorconfig`

**Files:**
- Create: `.editorconfig` (repository root, `root = true`)

**Interfaces:**
- Produces: The `.editorconfig` file that Task 2's `[tests/**/*.cs]` analyzer-relaxation section will be appended to, and which Tasks 3–5's `dotnet format` / build runs read from.

This task implements tasks.md 1.1, 1.2, and 1.3: a root file with general, C#, and frontend scoped sections. Frontend rules mirror Prettier/Vite defaults. C# rules use 4-space indent (matching the current source) and keep most rules at `suggestion`; only the small enforced core is `warning`/`error`. CA1707/CA1822 are relaxed for the whole `[tests/**/*.cs]` subtree in a section added later (Task 2), so leave room for that section here (it can be appended at the end).

- [x] **Step 1: Create the `.editorconfig`**

Write the following to `/home/bean/Projects/work/ticket-booking/.editorconfig`:

```editorconfig
root = true

# --- General defaults -------------------------------------------------------
[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true
indent_style = space

# --- C# ----------------------------------------------------------------------
[*.cs]
indent_style = space
indent_size = 4
tab_width = 4
insert_final_newline = true
charset = utf-8
end_of_line = lf

# File layout / usings
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
csharp_new_line_before_members_in_object_initializers = true
csharp_new_line_before_members_in_anonymous_types = true
csharp_new_line_between_query_expression_clauses = true
csharp_indent_case_contents_when_block = true

# Blank lines
csharp_blank_lines_after_namespace_declarations = 1
csharp_blank_lines_between_members = 1
csharp_blank_lines_between_using_statements = 0

# Naming: default (keep suggestion)
csharp_style_namespace_declarations = file_scoped:suggestion
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = true:suggestion
csharp_style_expression_bodied_methods = when_on_single_line:suggestion
csharp_style_expression_bodied_constructors = when_on_single_line:suggestion
csharp_style_expression_bodied_operators = when_on_single_line:suggestion
csharp_style_expression_bodied_properties = when_on_single_line:suggestion
csharp_style_expression_bodied_indexers = when_on_single_line:suggestion
csharp_style_expression_bodied_accessors = when_on_single_line:suggestion
csharp_style_expression_bodied_lambdas = when_on_single_line:suggestion
csharp_style_expression_bodied_local_functions = when_on_single_line:suggestion
csharp_style_throw_expression = true:suggestion
csharp_style_prefer_switch_expression = true:suggestion
csharp_style_prefer_pattern_matching = true:suggestion
csharp_style_prefer_conditional_expression_over_assignment = true:suggestion
csharp_style_prefer_conditional_expression_over_return = true:suggestion

# Accessibility modifiers
csharp_preferred_modifier_order = public,private,protected,internal,static,extern,new,virtual,abstract,sealed,override,readonly,unsafe,volatile,async:suggestion

# 'this.'
dotnet_style_qualification_for_field = false:suggestion
dotnet_style_qualification_for_property = false:suggestion
dotnet_style_qualification_for_method = false:suggestion
dotnet_style_qualification_for_event = false:suggestion

# Language keywords vs framework types
dotnet_style_predefined_type_for_locals_parameters_members = true:suggestion
dotnet_style_predefined_type_for_member_access = true:suggestion

# Modifier keywords
dotnet_style_require_accessibility_modifiers = for_non_interface_members:suggestion
dotnet_style_readonly_field = true:suggestion

# Code quality core (small enforced set)
dotnet_code_quality_unused_parameters = all:warning
dotnet_diagnostic.CA1031.severity = suggestion
dotnet_diagnostic.CA1305.severity = warning
dotnet_diagnostic.CA1816.severity = warning
dotnet_diagnostic.CA1852.severity = warning

# --- Frontend: TypeScript / JavaScript ---------------------------------------
[*.{ts,tsx,js,jsx}]
indent_style = space
indent_size = 2
tab_width = 2
charset = utf-8
insert_final_newline = true
end_of_line = lf

[*.{ts,tsx,js,jsx}]
# Prettier defaults so future Prettier/ESLint rollout aligns (Design 2.4)
# (multiple identical section headers are merged by editorconfig parsers;
#  keep all frontend knobs under the ts/tsx/js/jsx scope below)
```

> Note: multiple `[*.{ts,tsx,js,jsx}]` headers in one file are allowed and merged; this keeps the frontend rules visually grouped. Keep the file as produced.

- [x] **Step 2: Verify the file is well-formed and picked up by `dotnet format`**

Run:
```bash
dotnet format TicketBooking.slnx --verify-no-changes --include src/Backend/TicketBooking.Api src/Backend/TicketBooking.BuildingBlocks
```
Expected: command exits 0 and reports no files need formatting, confirming the `.editorconfig` is honored and current C# files are already conforming (4-space indent, final newline). `TicketBooking.BuildingBlocks/Class1.cs` currently lacks a final newline — if it is flagged here, that is expected and is fixed in Task 2.

> **Ruling (user):** `[*.cs]` uses `end_of_line = lf` (not crlf) so the repo stores LF without a `.gitattributes`, avoiding cross-platform churn. Task 4 re-ran `dotnet format` to revert the initial CRLF conversion.

- [x] **Step 3: Commit**

```bash
git add .editorconfig
git commit -m "chore: add root .editorconfig with C# and frontend formatting rules"
```

---

### Task 2: Enable warnings-as-errors and remediate the warning surface

**Files:**
- Modify: `Directory.Build.props` (PropertyGroup only)
- Modify: `.editorconfig` (append `[tests/**/*.cs]` section)
- Modify: `src/Backend/TicketBooking.BuildingBlocks/Class1.cs` (add trailing newline)
- Modify: `src/Backend/TicketBooking.Api/Program.cs` (seal `WeatherForecast`)
- Modify: `tests/TicketBooking.SystemTests/DependencyInjectionTests.cs` (CA1305)
- Modify: `tests/TicketBooking.SystemTests/Data/InMemoryDb.cs` (CA1816)

**Interfaces:**
- Consumes: The `.editorconfig` from Task 1 (its `[*.cs]` and new `[tests/**/*.cs]` sections drive final severities).
- Produces: A warning-clean tree under WAE, the exact state Tasks 3–5 verify. The `[tests/**/*.cs]` section here is what makes `dotnet build` under WAE pass for the test project.

This implements tasks.md 2.1 and 2.2: flip the analyzer/format/WAE switches and fix every warning the tree produces under `AnalysisMode=Recommended`. The warning list below was captured empirically by running `dotnet build` with WAE + Recommended on the current tree; it is the authoritative set to clear. The `.editorconfig` severity adjustments for CA1707 and CA1822 are the design-doc-prescribed mechanism (Design 2.3) for deliberate test conventions that we do not want to fail the build — they are *not* `<NoWarn>` suppressions.

**Step 1 — Update `Directory.Build.props`:**

Replace the `<PropertyGroup>` block in `/home/bean/Projects/work/ticket-booking/Directory.Build.props` with:

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

This matches Design 2.2 exactly. Do not change the other lines.

**Step 2 — Append the `[tests/**/*.cs]` relaxation section to `.editorconfig`:**

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

> If, after clearing every listed warning, `dotnet format --verify-no-changes` or the build still flags a rule in the test subtree that originates from a store-level (.editorconfig-independent) analyzer default, relax that specific `dotnet_diagnostic.<ID>.severity` here with a one-line comment rather than adding `<NoWarn>`.

**Step 3 — Fix each surfaced warning in source.**

The `dotnet build` after Step 1 + Step 2 fails (as errors) on the following. Fix all of them:

a. **`src/Backend/TicketBooking.Api/Program.cs:38` — CA1852** (`Type 'WeatherForecast' can be sealed`). Change:
```csharp
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
```
to:
```csharp
sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
```

b. **`src/Backend/TicketBooking.BuildingBlocks/Class1.cs` — missing final newline** (flagged by the `[*]` `insert_final_newline = true`). Append a trailing newline after the closing `}`. The resulting file is:
```csharp
namespace TicketBooking.BuildingBlocks;

public class Class1
{
}
```
(ends with a newline).

c. **`tests/TicketBooking.SystemTests/DependencyInjectionTests.cs:42` — CA1305** (`result.ToString()` locale). Change:
```csharp
        await db.SetAsync("result", result.ToString());
```
to:
```csharp
        await db.SetAsync("result", result.ToString(CultureInfo.InvariantCulture));
```
and add `using System.Globalization;` at the top of the file (before `namespace TicketBooking.SystemTests;`).

d. **`tests/TicketBooking.SystemTests/Data/InMemoryDb.cs:28` — CA1816** (`DisposeAsync` should call `GC.SuppressFinalize`). Change:
```csharp
    public ValueTask DisposeAsync()
    {
        // Simulate async teardown - e.g. closing connections, removing containers
        _store.Clear();
        return ValueTask.CompletedTask;
    }
```
to:
```csharp
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        // Simulate async teardown - e.g. closing connections, removing containers
        _store.Clear();
        return ValueTask.CompletedTask;
    }
```

For **CA1707** (underscore `_` in every SystemTests test-method name) and **CA1822** (`Calculator` `Add`/`Subtract`/`Multiply`/`Divide` can be static): these are *not* fixed in source — they are resolved by the `[tests/**/*.cs]` severity relaxations added in Step 2, because TUnit's underscore test naming and the DI-injectable `Calculator` are intentional. Do **not** rename test methods or make `Calculator` static.

**Step 4 — Run the build and confirm zero warnings/errors:**

Run:
```bash
dotnet build TicketBooking.slnx
```
Expected: `Build succeeded.` with `0 Warning(s)` and `0 Error(s)`, exit code 0.

If any remaining warning does not originate from our own code (e.g. an Aspire AppHost SDK/template artifact, generated NuGet tooling, or an unfixable third-party package), isolate it with a per-project `<NoWarn>` plus a one-line comment in the affected `.csproj`, then re-run the build to confirm 0 warnings. Per the design's scope guardrail, do **not** add a global `<NoWarn>` disabling WAE, and never suppress a warning that is ours to fix.

**Step 5 — Commit:**

```bash
git add Directory.Build.props .editorconfig \
  src/Backend/TicketBooking.Api/Program.cs \
  src/Backend/TicketBooking.BuildingBlocks/Class1.cs \
  tests/TicketBooking.SystemTests/DependencyInjectionTests.cs \
  tests/TicketBooking.SystemTests/Data/InMemoryDb.cs
git commit -m "chore: enable warnings-as-errors and remediate analyzer warnings"
```

---

### Task 3: Negative proof that warnings-as-errors is active

**Files:**
- Modify (temporarily): `src/Backend/TicketBooking.Api/Program.cs`

**Interfaces:**
- Consumes: The WAE-enabled `Directory.Build.props` from Task 2. Produces: an explicit, repeatable proof the behavioral change actually holds.

This implements tasks.md 2.3: deliberately introduce a trivial compiler warning and show `dotnet build` fails, then remove it.

- [x] **Step 1: Introduce a deliberate warning**

Add an unused local at the top of `Program.cs` (after `var builder = WebApplication.CreateBuilder(args);`):

```csharp
int _deliberateUnusedLocal = 0;
```

This produces compiler warning CS0219 ("The variable ... assigned but never used"). Under `TreatWarningsAsErrors=true` it becomes an error.

- [x] **Step 2: Confirm the build fails**

Run:
```bash
dotnet build TicketBooking.slnx
```
Expected: `Build FAILED.` with the message `CS0219` reported as an **error** (e.g. `error CS0219`), and a non-zero exit code. This proves warnings-as-errors is active.

- [x] **Step 3: Remove the deliberate warning**

Delete the `int _deliberateUnusedLocal = 0;` line added in Step 1 so `Program.cs` returns to its Task 2 state.

- [x] **Step 4: Confirm the build is green again**

Run:
```bash
dotnet build TicketBooking.slnx
```
Expected: `Build succeeded.` with `0 Warning(s)`, `0 Error(s)`, exit code 0.

- [x] **Step 5: Commit**

```bash
git add src/Backend/TicketBooking.Api/Program.cs
git commit -m "test: prove warnings-as-errors breaks the build on a deliberate warning"
```

> The commit records only the restored `Program.cs`; the temporary breaking edit is intentionally not committed.

---

### Task 4: Formatting conforms via `dotnet format --verify-no-changes`

**Files:**
- None created/modified directly. Read-only verification.

**Interfaces:**
- Consumes: The `.editorconfig` (Tasks 1–2) and the source files (Tasks 2–3). Produces: confirmation that every C# file matches the defined formatting.

This implements tasks.md 3.1.

- [x] **Step 1: Run the format verification across the solution**

Run:
```bash
dotnet format TicketBooking.slnx --verify-no-changes
```
Expected: exits 0 and reports `Format` complete with no files needing changes (`dotnet format` exits non-zero if any file would be reformatted).

- [x] **Step 2: If the verify fails, apply formatting, then re-verify**

Only if Step 1 reports files to change: run `dotnet format TicketBooking.slnx`, review the `git diff` to confirm the changes are formatting-only, commit if the output is accepted, then re-run Step 1 until it returns exit 0.

- [x] **Step 3: Commit any formatting fixups**

If Step 2 produced changes:
```bash
git add -u
git commit -m "style: apply .editorconfig formatting with dotnet format"
```
If Step 1 already passed, there is nothing to commit in this task.

---

### Task 5: Final clean rebuild verification

**Files:**
- None created/modified. Read-only verification.

**Interfaces:**
- Consumes: All prior tasks. Produces: the end-state evidence required by tasks.md 3.2 and the design's testing strategy (Design 3).

- [ ] **Step 1: Clean and rebuild the full solution**

Run:
```bash
dotnet build TicketBooking.slnx -t:Rebuild
```
Expected: `Build succeeded.` with `0 Warning(s)`, `0 Error(s)`, exit code 0. This recompiles from scratch so no incremental-cache artifact masks stale warnings.

- [ ] **Step 2: Confirm the analyzer breadth did not cause unexpected failures**

Review the build output: no unexpected `error`/`warning` lines; only the enabled `Recommended`-mode rule set is active with no new severities forcing suppressible churn (Design 3 row "Analyzer breadth sane").

- [ ] **Step 3: Final commit (only if the clean rebuild produced any source change)**

If Step 1's rebuild surfaced no changes (expected), there is nothing to commit. If the rebuild revealed any remaining warning requiring a fix, apply it as in Task 2 Step 3 (fix in source, or relax severity in `.editorconfig` per Design 2.3), re-run Step 1, then:
```bash
git add -A
git commit -m "chore: final warning-clean build for code-analysis-setup"
```

## Self-Review / Coverage Map

- tasks.md 1.1 (root `.editorconfig` picked up by `dotnet format`) → Task 1 Steps 1–2.
- tasks.md 1.2 (C# style rules, sample reformats clean) → Task 1 Step 2 (verify no C# changes), Task 4.
- tasks.md 1.3 (frontend TS/JS rules, no C# conflict via path-scoped sections) → Task 1 Step 1 (`[*.{ts,tsx,js,jsx}]` sections, distinct from `[*.cs]`).
- tasks.md 2.1 (Directory.Build.props WAE switches) → Task 2 Step 1.
- tasks.md 2.2 (fix every surfaced warning, justified `NoWarn` only) → Task 2 Steps 2–4.
- tasks.md 2.3 (deliberate warning fails build, then removed) → Task 3.
- tasks.md 3.1 (`dotnet format --verify-no-changes`) → Task 4.
- tasks.md 3.2 (clean rebuild, exit 0, zero warnings) → Task 5.

Empirically-observed warning set that Task 2 renders irrelevant under WAE + Recommended: CA1852 (Api), CA1707 (SystemTests underscore methods), CA1822 (SystemTests Calculator), CA1305 (SystemTests `ToString()`), CA1816 (InMemoryDb) — all are either fixed in source or consciously relaxed for test conventions, matching Design 2.3.
