# Development

Use this file for dependency setup, local execution, and common environment failures.

## Prerequisites

The repository requires .NET 10 SDK, Aspire CLI 13.5 or newer, Node.js compatible with Vite 8, and pnpm 11 or newer. Docker is required only for API container-build verification.

Check installed tools with:

```bash
dotnet --version
aspire --version
node --version
pnpm --version
```

## Restore Dependencies

Run from the repository root:

```bash
dotnet restore TicketBooking.slnx
pnpm --dir src/Frontend/public-web install --frozen-lockfile
pnpm --dir src/Frontend/backoffice-web install
```

There is no root pnpm workspace. Do not run `pnpm install` at the repository root or combine the frontend packages without an explicit architecture decision. `public-web` has a committed `pnpm-lock.yaml`; `backoffice-web` currently does not. After a lockfile is added there, use `--frozen-lockfile` for both applications.

Add NuGet `PackageReference` entries without versions in `.csproj` files and add their versions to `Directory.Packages.props`.

## Run Locally

Start the complete system from `src/Aspire/TicketBooking.AppHost`:

```bash
aspire start --non-interactive
```

For a worktree or parallel instance, use:

```bash
aspire start --isolated --non-interactive
```

Aspire assigns ports dynamically. Obtain URLs from CLI output or the Dashboard; do not hard-code local ports in source or tests. Stop the same AppHost with:

```bash
aspire stop --non-interactive
```

Do not launch AppHost with `dotnet run`. In automation, use `aspire wait <resource>` before accessing a resource. After changing one resource, prefer its HMR/watch behavior or `aspire resource` over restarting the entire graph.

Run frontends independently from the repository root with:

```bash
pnpm --dir src/Frontend/public-web dev
pnpm --dir src/Frontend/backoffice-web dev
```

Both use Vite HMR. The API can run independently with `dotnet run --project src/Backend/TicketBooking.Api`, but that mode does not provide Aspire service discovery or the shared resource model.

## Known Aspire Frontend Limitation

`AddViteApp` currently invokes `npm install` even though the frontends use pnpm. After pnpm installation, npm may fail with `EUNSUPPORTEDPROTOCOL` on `workspace:*` under pnpm's `node_modules/.ignored`, preventing that frontend resource from starting.

Do not mix npm and pnpm lockfiles or commit an Aspire-generated `package-lock.json`. Until package-manager integration is fixed, verify frontends with their pnpm commands and run the API through its project command.

## Diagnostics

- If Aspire cannot find AppHost, run it from `src/Aspire/TicketBooking.AppHost` or pass `--apphost` explicitly.
- If another local AppHost conflicts, use `aspire start --isolated --non-interactive` rather than fixed ports.
- If a Vite installer fails with `EUNSUPPORTEDPROTOCOL`, inspect `aspire logs <resource>-installer`; mixed package-manager state may be the cause.
- If frontend dependencies disagree with a lockfile, identify the uncommitted `package.json` change instead of deleting the lockfile.
