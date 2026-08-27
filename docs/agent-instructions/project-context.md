# Project Context

Use this file when determining repository scope, current implementation state, or where a change belongs.

## Current State

TicketBooking targets event publishing, concurrency-safe booking, payment, and electronic ticket issuance. The backend is intended to be an ASP.NET Core modular monolith with isolated business modules and two React interfaces:

- `public-web` is the customer application.
- `backoffice-web` is the staff application.
- `TicketBooking.Api` is the HTTP API and future module composition root.
- `TicketBooking.AppHost` provides local orchestration through .NET Aspire.
- `TicketBooking.ServiceDefaults` provides shared health checks, service discovery, resilience, and OpenTelemetry configuration to services that opt in.

The repository is at an early stage. AppHost currently starts a minimal API with the template `/weatherforecast` endpoint and two template Vite applications. PostgreSQL, Gateway/YARP, Worker, DatabaseMigrator, payments, complete business modules, authentication, and production deployment are not implemented. Do not describe planned components as existing; check `docs/plan.md` and the source.

## Stack

- .NET 10, C#, and ASP.NET Core
- .NET Aspire 13.5
- React 19, TypeScript 6, and Vite 8
- pnpm 11, with independently managed frontend applications
- TUnit 1.6 on Microsoft Testing Platform
- Oxlint for frontend linting
- Central NuGet package versions in `Directory.Packages.props`

## Repository Layout

| Path | Purpose |
| --- | --- |
| `src/Aspire/TicketBooking.AppHost` | Aspire resource graph; entry point is `AppHost.cs` |
| `src/Aspire/TicketBooking.ServiceDefaults` | Shared observability, health check, and HTTP client defaults |
| `src/Backend/TicketBooking.Api` | Minimal ASP.NET Core API |
| `src/Backend/TicketBooking.BuildingBlocks` | Technical primitives genuinely shared across modules |
| `src/Backend/Modules` | Contracts and implementations for isolated business modules |
| `src/Frontend/public-web` | Customer React/Vite frontend |
| `src/Frontend/backoffice-web` | Staff React/Vite frontend |
| `tests` | Unit, integration, architecture, and system test projects |
| `docs` | Domain analysis, target architecture, backlog, and plans |
| `TicketBooking.slnx` | Root .NET solution, including both `.esproj` files |

Older documents may use `admin-web`; the actual project and resource name is `backoffice-web`. Follow the file structure and `AppHost.cs`, and correct related documentation drift when changing that area.

Documents may describe target state ahead of implementation. Treat `.slnx`, `.csproj`, `package.json`, and source files as authoritative for current dependencies and behavior.
