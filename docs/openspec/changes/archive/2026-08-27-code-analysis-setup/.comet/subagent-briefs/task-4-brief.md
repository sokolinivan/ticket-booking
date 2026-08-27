# Task 4 Brief — Formatting conforms via `dotnet format --verify-no-changes`

**Plan task:** Task 4 "Formatting conforms via `dotnet format --verify-no-changes`" (from docs/superpowers/plans/2026-08-27-code-analysis-setup.md).

**Language:** Use English (configured Comet artifact language = `en`).

**Goal:** Confirm every C# file matches the defined `.editorconfig` formatting by running `dotnet format --verify-no-changes` across the solution. Implements OpenSpec tasks.md 3.1.

## Allowed scope
- Read-only verification. You MAY modify C# source files ONLY if `--verify-no-changes` reports files needing reformatting (then apply `dotnet format`, confirm the diff is formatting-only, and commit).
- Do NOT check off any plan/OpenSpec checkboxes.

## Steps

**Step 1 — Run the format verification.** From the repo root:
```bash
dotnet format TicketBooking.slnx --verify-no-changes
```
Expected: exits 0 and reports "Format" complete with no files needing changes. If it exits 0, there is nothing to fix — record the output and proceed to the report (no commit needed).

**Step 2 — If verify fails, apply formatting, then re-verify.** Only if Step 1 reports files to change:
```bash
dotnet format TicketBooking.slnx
```
Then review `git diff` to confirm the changes are formatting-only (whitespace, newlines, braces, usings order — NOT logic). If the diff contains any semantic/logic change, STOP and report BLOCKED with the details instead of committing.

**Step 3 — Commit any formatting fixups.** If Step 2 produced formatting-only changes:
```bash
git add -u
git commit -m "style: apply .editorconfig formatting with dotnet format"
```
Do NOT stage `docs/openspec/changes/.../.comet/*` or the plan file. If Step 1 already passed, commit nothing.

## Report contract

Return status `DONE | DONE_WITH_CONCERNS | BLOCKED | NEEDS_CONTEXT` and include:
- status
- Step 1 result (`dotnet format TicketBooking.slnx --verify-no-changes` exit code + summary)
- whether formatting was applied (Step 2) and committed (hash if so), or nothing needed
- if applied, confirmation that the diff was formatting-only
- concerns (if any)
- risk signals hit (from the list — expect none for formatting)

Do NOT load or run any Comet/openai skill — implement directly.
