# Task 3 Brief — Negative proof that warnings-as-errors is active

**Plan task:** Task 3 "Negative proof that warnings-as-errors is active" (from docs/superpowers/plans/2026-08-27-code-analysis-setup.md).

**Language:** Use English (configured Comet artifact language = `en`).

**Goal:** Prove warnings-as-errors is actually active by deliberately introducing a trivial compiler warning, showing `dotnet build` fails, then removing it and showing the build is green again. Implements OpenSpec tasks.md 2.3.

## Allowed scope
- Temporarily modify ONLY `src/Backend/TicketBooking.Api/Program.cs`, then restore it.
- Do NOT touch any other file. Do NOT check off any plan/OpenSpec checkboxes.

## Steps

**Step 1 — Introduce a deliberate warning.** In `/home/bean/Projects/work/ticket-booking/src/Backend/TicketBooking.Api/Program.cs`, right after `var builder = WebApplication.CreateBuilder(args);` add the line:
```csharp
int _deliberateUnusedLocal = 0;
```
This produces compiler warning CS0219 ("variable assigned but never used"), which under `TreatWarningsAsErrors=true` becomes an error.

**Step 2 — Confirm the build fails.** Run:
```bash
dotnet build TicketBooking.slnx
```
Expected: `Build FAILED.` with CS0219 reported as an **error** (e.g. `error CS0219`), non-zero exit code. This is the proof.

**Step 3 — Remove the deliberate warning.** Delete the `int _deliberateUnusedLocal = 0;` line so `Program.cs` returns exactly to its committed state. Verify it is byte-identical to HEAD's version:
```bash
git diff --exit-code -- src/Backend/TicketBooking.Api/Program.cs
```
This must exit 0 (no diff). Do NOT commit any version that still contains the deliberate line.

**Step 4 — Confirm the build is green again.** Run:
```bash
dotnet build TicketBooking.slnx
```
Expected: `Build succeeded.` with `0 Warning(s)`, `0 Error(s)`, exit code 0.

**Step 5 — Commit.** Since `Program.cs` is restored to its exact committed state (no diff), running `git add src/Backend/TicketBooking.Api/Program.cs` + `git commit` would create an empty commit. Instead, commit the plan/task progress only if there is nothing to commit from source; if `git status` shows only the restored-but-unchanged file, do NOT create an empty commit — instead report that no source change was committed (the evidence lives in the build output). If for any reason the file did change, stage and commit it with message `test: prove warnings-as-errors breaks the build on a deliberate warning`.

NOTE: Capture the two build outputs (failure + success) in your report as the evidence.

## Report contract

Return status `DONE | DONE_WITH_CONCERNS | BLOCKED | NEEDS_CONTEXT` and include:
- status
- whether a source commit was created (and its hash if so) — expect: no source commit, file restored
- Step 2 build result (FAILED with error CS0219, non-zero exit code) — copy the relevant error line
- Step 4 build result (succeeded, 0 warnings, 0 errors, exit code 0)
- `git diff --exit-code -- src/Backend/TicketBooking.Api/Program.cs` result (must be clean)
- concerns (if any)
- risk signals hit (from the list: cross-module, security, concurrency, migration, public API, diff>200 lines — expect none)

Do NOT load or run any Comet/openai skill — implement directly. Do not modify anything outside `Program.cs` (temporarily), and do not leave any change in `Program.cs` relative to HEAD.
