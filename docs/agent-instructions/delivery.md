# Delivery and Collaboration

Use this file for containers, deployment, commits, pull requests, and final handoff.

## Containers and Deployment

`src/Backend/TicketBooking.Api/Dockerfile` contains a multi-stage Linux API build, but production deployment, Compose or Kubernetes manifests, and CI/CD pipelines are not configured. Do not claim deployment readiness merely because the Dockerfile exists.

The Dockerfile currently cannot restore because it copies `.csproj` files before copying `Directory.Packages.props`, leaving the centrally managed `Microsoft.AspNetCore.OpenApi` version unavailable (`NU1015`). After fixing the `COPY` order, verify from the repository root:

```bash
docker build -f src/Backend/TicketBooking.Api/Dockerfile -t ticketbooking-api:local .
```

Docker restore must see both `Directory.Packages.props` and `Directory.Build.props`. Publish through Aspire only after selecting and configuring a concrete deployment target.

## Commits and Pull Requests

No GitHub Actions or other CI is currently configured. Before opening a pull request, run the complete checks in [Verification and Testing](verification.md).

- Follow the repository's short conventional commit prefixes, such as `feat:`, `chore:`, and `docs:`, unless the user requests another style.
- Keep each commit focused on one logical goal; exclude unrelated and generated files.
- In pull request descriptions, state the changed area, checks run, migrations or configuration changes, and known limitations.

## Agent Collaboration

- Preserve unrelated user changes in a dirty worktree.
- Inspect nearby source, project files, and relevant documentation before editing.
- Make the smallest coherent change and avoid unrelated refactoring.
- Add or update tests for changed behavior without waiting for a separate request.
- In the final report, list checks actually run and identify any unavailable or skipped verification.
