# Identity Domain And Persistence Verification

## Scope

Task 13 verifies OpenSpec task 5.2 and the Identity domain, PostgreSQL persistence, deployment topology, and repository-wide static checks implemented by Tasks 1-12. Verification repaired only two change-caused formatting defects: a generated migration BOM and architecture-test import ordering.

## Environment

- Arch Linux, .NET SDK 10.0.111, TUnit 1.6.28, Microsoft Testing Platform 2.0.2.
- Docker 29.7.2 was running; PostgreSQL integration tests used real Docker PostgreSQL.
- pnpm 11.22.0, TypeScript 6.0.3, and Vite 8.2.2.
- Aspire CLI 13.5.2 with AppHost SDK 13.5.3. The session's `https_proxy`, `SSL_CERT_FILE`, and `REQUESTS_CA_BUNDLE` overrides intercepted or replaced trust for DCP's loopback certificate, so successful Aspire lifecycle commands unset those variables for that process only.

## Commands

```bash
dotnet restore TicketBooking.slnx
dotnet format TicketBooking.slnx --verify-no-changes --no-restore
dotnet format TicketBooking.slnx --no-restore
dotnet format TicketBooking.slnx --verify-no-changes --no-restore
dotnet build TicketBooking.slnx --no-restore
dotnet run --project tests/TicketBooking.UnitTests --no-build -- --minimum-expected-tests 1
dotnet run --project tests/TicketBooking.ArchitectureTests --no-build -- --minimum-expected-tests 1
dotnet run --project tests/TicketBooking.IntegrationTests --no-build -- --minimum-expected-tests 1
dotnet run --project tests/TicketBooking.SystemTests --no-build
POSTGRES_USER=test POSTGRES_PASSWORD=test POSTGRES_DB=ticketbooking docker compose config --quiet
aspire start --isolated --non-interactive
aspire stop --non-interactive
aspire doctor --non-interactive
env -u https_proxy aspire start --isolated --non-interactive
env -u https_proxy -u SSL_CERT_FILE -u REQUESTS_CA_BUNDLE aspire doctor --non-interactive
env -u https_proxy -u SSL_CERT_FILE -u REQUESTS_CA_BUNDLE aspire start --isolated --non-interactive
env -u https_proxy -u SSL_CERT_FILE -u REQUESTS_CA_BUNDLE aspire ps --include-hidden
env -u https_proxy -u SSL_CERT_FILE -u REQUESTS_CA_BUNDLE aspire ps --non-interactive --format Json
aspire describe --help
aspire wait --help
env -u https_proxy -u SSL_CERT_FILE -u REQUESTS_CA_BUNDLE aspire describe --include-hidden --non-interactive --format Table
env -u https_proxy -u SSL_CERT_FILE -u REQUESTS_CA_BUNDLE aspire wait ticketbooking --status healthy --timeout 120 --non-interactive
env -u https_proxy -u SSL_CERT_FILE -u REQUESTS_CA_BUNDLE aspire wait ticketbooking-api --status healthy --timeout 120 --non-interactive
env -u https_proxy -u SSL_CERT_FILE -u REQUESTS_CA_BUNDLE aspire stop --non-interactive
pnpm --dir src/Frontend/public-web lint
pnpm --dir src/Frontend/public-web build
pnpm --dir src/Frontend/backoffice-web lint
pnpm --dir src/Frontend/backoffice-web build
git diff --check
git status --short
git diff --stat 15bdc95546713a8af47ffd2a9962f99e648d05cd
```

## Results

- Restore: exit 0; 3 projects restored and 8 of 11 already current.
- Initial format verification: failed with `CHARSET` on `20260827224854_InitialIdentity.cs` and `IMPORTS` on `IdentityModuleArchitectureTests.cs`. Formatter changed only those scoped defects; fresh verification then exited 0.
- Solution build: exit 0, 0 warnings, 0 errors.
- Unit tests: 50 passed, 0 failed, 0 skipped.
- Architecture tests: 5 passed, 0 failed, 0 skipped.
- PostgreSQL integration tests: 19 passed, 0 failed, 0 skipped in 9.443 seconds.
- System tests: 27 passed, 0 failed, 0 skipped.
- Compose config: exit 0 with injected test-only credentials and no output.
- Aspire: the unmodified environment failed DCP startup with a proxy-generated 502; unsetting only `https_proxy` then exposed the trust override and DCP exited. `aspire doctor` with all three overrides removed reported 5 passed, 3 warnings, 0 failed, and the AppHost then started successfully.
- Aspire topology: `postgres`, `ticketbooking`, and `ticketbooking-api` were Running and Healthy. Explicit waits for `ticketbooking` and `ticketbooking-api` returned Healthy. Hidden installer resources were visible; frontend resources were Waiting while installers ran, matching the known unrelated installer limitation. AppHost stopped successfully.
- Frontend lint/build: both public-web and backoffice-web linted successfully; both TypeScript/Vite production builds succeeded with 20 modules transformed.

## Skipped Or Unavailable

- No required check was skipped.
- Installed Aspire CLI 13.5.2 does not accept `aspire ps --include-hidden`; it returned an unrecognized-argument error. The supported `aspire describe --include-hidden --non-interactive --format Table` command supplied equivalent resource evidence.
- Aspire CLI 13.5.2 is one patch behind the 13.5.3 AppHost SDK. No tool upgrade or user trust-store change was made.
- Frontend checks auto-installed local dependencies because app lockfiles are not tracked. Generated `dist/`, `package-lock.json`, and `pnpm-lock.yaml` artifacts were not committed.
