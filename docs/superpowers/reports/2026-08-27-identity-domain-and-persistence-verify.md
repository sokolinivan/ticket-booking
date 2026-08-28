# Identity Domain And Persistence Verification

## Scope

Task 13 verifies OpenSpec task 5.2 and the Identity domain, PostgreSQL persistence, deployment topology, and repository-wide static checks implemented by Tasks 1-12. Verification repaired two change-caused formatting defects and one change-caused container restore failure. The Dockerfile predates Tasks 1-12, but Task 1's new API reference to Identity Core made its existing single-project restore layer incomplete; the Task 13 brief explicitly permits the smallest repair for a change-caused verification failure.

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
POSTGRES_USER=task13_user POSTGRES_PASSWORD=task13_temp_20260828 POSTGRES_DB=task13_db docker compose -p ticketbooking-task13-verify up -d --build --wait --wait-timeout 180
POSTGRES_USER=task13_user POSTGRES_PASSWORD=task13_temp_20260828 POSTGRES_DB=task13_db docker compose -p ticketbooking-task13-verify up -d --build --wait --wait-timeout 180
POSTGRES_USER=task13_user POSTGRES_PASSWORD=task13_temp_20260828 POSTGRES_DB=task13_db docker compose -p ticketbooking-task13-verify ps --format json
POSTGRES_USER=task13_user POSTGRES_PASSWORD=task13_temp_20260828 POSTGRES_DB=task13_db docker compose -p ticketbooking-task13-verify exec -T ticketbooking-api printenv ConnectionStrings__ticketbooking
POSTGRES_USER=task13_user POSTGRES_PASSWORD=task13_temp_20260828 POSTGRES_DB=task13_db docker compose -p ticketbooking-task13-verify exec -T postgres psql -U task13_user -d task13_db -v ON_ERROR_STOP=1 -c "CREATE TABLE task13_volume_sentinel (value text PRIMARY KEY); INSERT INTO task13_volume_sentinel VALUES ('persists-across-recreation');"
POSTGRES_USER=task13_user POSTGRES_PASSWORD=task13_temp_20260828 POSTGRES_DB=task13_db docker compose -p ticketbooking-task13-verify rm -s -f postgres
POSTGRES_USER=task13_user POSTGRES_PASSWORD=task13_temp_20260828 POSTGRES_DB=task13_db docker compose -p ticketbooking-task13-verify up -d postgres --wait --wait-timeout 120
POSTGRES_USER=task13_user POSTGRES_PASSWORD=task13_temp_20260828 POSTGRES_DB=task13_db docker compose -p ticketbooking-task13-verify exec -T postgres psql -U task13_user -d task13_db -Atqc "SELECT value FROM task13_volume_sentinel;"
POSTGRES_USER=task13_user POSTGRES_PASSWORD=task13_temp_20260828 POSTGRES_DB=task13_db docker compose -p ticketbooking-task13-verify down --volumes --rmi local --remove-orphans
docker ps -a --filter label=com.docker.compose.project=ticketbooking-task13-verify --format '{{.Names}}' && docker volume ls --filter name=ticketbooking-task13-verify --format '{{.Name}}' && docker network ls --filter name=ticketbooking-task13-verify --format '{{.Name}}' && docker image ls ticketbooking-task13-verify-ticketbooking-api --format '{{.Repository}}:{{.Tag}}'
docker build --no-cache -f src/Backend/TicketBooking.Api/Dockerfile -t ticketbooking-api:task13-review .
docker image rm ticketbooking-api:task13-review
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
- Initial isolated Compose runtime acceptance: failed during API image restore because the Dockerfile copied only `TicketBooking.Api.csproj`; `Directory.Packages.props` and the newly referenced Identity Core project were unavailable, producing `NU1015` for `Microsoft.AspNetCore.OpenApi` and a skipped project-reference warning. Although the Dockerfile predates Tasks 1-12, Task 1's project reference caused this verification failure. The Task 13-permitted repair copies `Directory.Build.props`, `Directory.Packages.props`, and only the API and Identity Core project files before restore, then copies all sources, preserving dependency-layer caching.
- Repeated isolated Compose runtime acceptance: exit 0. The API image restored, built, and published successfully with 0 warnings and 0 errors. Compose created only the `ticketbooking-task13-verify` project resources; PostgreSQL reached `healthy` and the API reached `running` after the healthy-database dependency completed.
- API connection setting: `printenv ConnectionStrings__ticketbooking` returned `Host=postgres;Port=5432;Database=task13_db;Username=task13_user;Password=task13_temp_20260828`, proving Compose supplied the equivalent database capability to the API.
- Named-volume persistence: sentinel table creation and insert returned `CREATE TABLE` and `INSERT 0 1`. PostgreSQL was stopped and removed without deleting its volume, recreated healthy, and the query returned `persists-across-recreation`.
- Compose cleanup: `down --volumes --rmi local --remove-orphans` removed both containers, the isolated network, `ticketbooking-task13-verify_postgres-data`, and the locally built API image. The subsequent filtered container, volume, network, and image check returned no output.
- Final Dockerfile review verification: a fresh `--no-cache` image build restored both the API and Identity Core projects, then built and published successfully with 0 warnings and 0 errors. The temporary `ticketbooking-api:task13-review` image was removed afterward.
- Aspire: the unmodified environment failed DCP startup with a proxy-generated 502; unsetting only `https_proxy` then exposed the trust override and DCP exited. `aspire doctor` with all three overrides removed reported 5 passed, 3 warnings, 0 failed, and the AppHost then started successfully.
- Aspire topology: `postgres`, `ticketbooking`, and `ticketbooking-api` were Running and Healthy. Explicit waits for `ticketbooking` and `ticketbooking-api` returned Healthy. Hidden installer resources were visible; frontend resources were Waiting while installers ran, matching the known unrelated installer limitation. AppHost stopped successfully.
- Frontend lint/build: both public-web and backoffice-web linted successfully; both TypeScript/Vite production builds succeeded with 20 modules transformed.
- Scope checks: fresh `git diff --check` exited 0 with no output. Before report edits, `git status --short` showed the pre-existing modified `.comet/subagent-progress.md` plus the API Dockerfile repair. `git diff --stat 15bdc95546713a8af47ffd2a9962f99e648d05cd` reported 75 files changed, 5,237 insertions, and 3 deletions; inspection found the Identity persistence implementation, tests, deployment topology, and change artifacts/reports, plus the pre-existing Dockerfile necessarily repaired after Task 1 added the Identity Core reference. No `.env`, database, `bin/`, `obj/`, or `dist/` files were in scope.

## Skipped Or Unavailable

- No required check was skipped.
- Installed Aspire CLI 13.5.2 does not accept `aspire ps --include-hidden`; it returned an unrecognized-argument error. The supported `aspire describe --include-hidden --non-interactive --format Table` command supplied equivalent resource evidence.
- Aspire CLI 13.5.2 is one patch behind the 13.5.3 AppHost SDK. No tool upgrade or user trust-store change was made.
- Frontend checks auto-installed local dependencies because app lockfiles are not tracked. Generated `dist/`, `package-lock.json`, and `pnpm-lock.yaml` artifacts were not committed.
- The Compose credentials recorded above were temporary acceptance-only values, supplied at invocation rather than embedded in deployment source. The isolated named volume that contained them and the sentinel was deleted during cleanup.
