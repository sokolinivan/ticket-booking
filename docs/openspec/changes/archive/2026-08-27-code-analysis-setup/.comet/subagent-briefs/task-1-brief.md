# Task 1 Brief — Create the root `.editorconfig`

**Plan task:** Task 1 "Create the root `.editorconfig`" (from docs/superpowers/plans/2026-08-27-code-analysis-setup.md).

**Language:** Use English for any notes/report (configured Comet artifact language = `en`).

**Goal:** Create a repository-root `.editorconfig` at `/home/bean/Projects/work/ticket-booking/.editorconfig` with general, C#, and frontend (TS/JS) scoped sections. This implements OpenSpec tasks.md 1.1, 1.2, 1.3.

## Allowed scope
- Create ONLY the file `/home/bean/Projects/work/ticket-booking/.editorconfig`.
- Do NOT modify any other file (Directory.Build.props, source, tests, csproj).
- Do NOT check off any plan/OpenSpec task checkboxes.

## Exact content to write

Write the following verbatim to `/home/bean/Projects/work/ticket-booking/.editorconfig`:

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
end_of_line = crlf

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
```

Notes (do not change behavior):
- Multiple `[*.{ts,tsx,js,jsx}]` headers are allowed and merged by editorconfig parsers; keep the file as produced above.
- Do NOT add the `[tests/**/*.cs]` section in this task — it is added by Task 2.

## Verify

Run (from repo root):
```bash
dotnet format TicketBooking.slnx --verify-no-changes --include src/Backend/TicketBooking.Api src/Backend/TicketBooking.BuildingBlocks
```
Expected: exits 0 and reports no files need formatting, confirming `.editorconfig` is honored and current C# files are already conforming. `TicketBooking.BuildingBlocks/Class1.cs` currently lacks a final newline — if it is flagged, that is expected and will be fixed in Task 2; record it but do NOT fix it in this task.

## Commit

```bash
git add .editorconfig
git commit -m "chore: add root .editorconfig with C# and frontend formatting rules"
```

## Report contract

Return a report with status `DONE | DONE_WITH_CONCERNS | BLOCKED | NEEDS_CONTEXT`, and include: commit hash, changed files, the verify command output summary, whether the build/verify passed, and any concerns. Also report whether this task hits any risk signal from this list:
- Cross-module / cross-subsystem coordinated change
- Security-sensitive surface (auth, crypto, SQL, secrets)
- Concurrency / locks / shared mutable state
- Data or schema migration
- Public API contract or external interface change
- Single-task diff exceeds 200 lines
(For this formatting-only task, expect: none.)

Do NOT load or run any Comet/openai skill. Implement directly. Do NOT implement or fix anything outside `.editorconfig`.
