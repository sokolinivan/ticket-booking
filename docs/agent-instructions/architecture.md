# Architecture

Use this file when changing module boundaries, dependencies, AppHost, or shared abstractions.

## Dependency Rules

- Treat `TicketBooking.Api` and future Worker and DatabaseMigrator applications as composition roots. Keep business rules out of HTTP endpoints and AppHost.
- Publish a module's cross-module API through its `.Contracts` project. Keep implementation details in `.Core`; other modules must not depend on them directly.
- Put only stable technical primitives required by multiple modules in `TicketBooking.BuildingBlocks`; do not create a shared business model there.
- Do not access another module's tables or future PostgreSQL schema directly. Communicate through contracts, events, and identifiers.
- Keep dependencies directed inward. Domain logic must not depend on Aspire, ASP.NET Core, frontend code, YARP, or the payment emulator.
- Keep customer and staff frontends independent; do not connect them with direct relative imports.

## Change Rules

- Before changing AppHost, check the current Aspire API with `aspire docs search` and `aspire docs api search`; do not guess from outdated APIs.
- If implementation changes accepted behavior or module boundaries, update the relevant documents under `docs/` in the same change.
- Study nearby source, project files, and relevant documents before editing. Do not infer current implementation solely from backlog or target-state documents.
- Make the smallest coherent change and avoid unrelated refactoring.

Documents can lead implementation. Treat `.slnx`, `.csproj`, `package.json`, and source files as authoritative for the current dependency graph and behavior.
