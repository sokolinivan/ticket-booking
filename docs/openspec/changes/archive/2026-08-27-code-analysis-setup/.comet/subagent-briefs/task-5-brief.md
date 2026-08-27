# Task 5 Brief — Final clean rebuild verification

**Plan task:** Task 5 "Final clean rebuild verification" (from docs/superpowers/plans/2026-08-27-code-analysis-setup.md).

**Language:** Use English (configured Comet artifact language = `en`).

**Goal:** Prove the whole solution rebuilds cleanly from scratch with warnings-as-errors active — the final end-state evidence for OpenSpec tasks.md 3.2.

## Allowed scope
- Read-only verification across the full solution. Do NOT modify source.
- You MAY commit ONLY if a clean rebuild produced a source change (it should not). Otherwise commit nothing.
- Do NOT check off any plan/OpenSpec checkboxes.

## Steps

**Step 1 — Clean and rebuild the full solution.** From the repo root:
```bash
dotnet clean TicketBooking.slnx -v q
dotnet build TicketBooking.slnx
```
Expected: `Build succeeded.` with `0 Warning(s)` and `0 Error(s)`, exit code 0. Report the exact warning/error counts and exit code.

**Step 2 — Confirm the analyzer breadth did not cause unexpected failures.** The full `AnalysisMode=Recommended` breadth should have already been validated in Tasks 2–4. Confirm here that the clean rebuild produces the same 0-warning/0-error result (i.e., nothing is order/state dependent). Record the summary.

**Step 3 — Final commit (only if the clean rebuild produced any source change).** Check `git status --short`. If source changed (it should not), stage the source only and commit as `chore: final clean rebuild`. If nothing changed (expected), commit nothing.

## Report contract

Return status `DONE | DONE_WITH_CONCERNS | BLOCKED | NEEDS_CONTEXT` and include:
- status
- Step 1 `dotnet clean` and `dotnet build` summaries (warnings/errors + exit code)
- whether a source commit was created (hash if so) — expect: no
- `git status --short` result (expect only untracked `.workspaces/` and nothing else)
- concerns (if any)
- risk signals hit (expect none)

Do NOT load or run any Comet/openai skill — implement directly.
