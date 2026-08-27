# Architecture Tests: правила зависимостей модулей

## Capability

Исполняемый набор архитектурных тестов в проекте `TicketBooking.ArchitectureTests`, проверяющий недопустимые project/namespace dependencies модульного монолита TicketBooking на основе namespace-префиксов.

## Цель

Автоматически защищать границы модульного монолита: изоляцию бизнес-модулей, направление слоёв, чистоту домена и направление dependencies composition roots, без трудоёмкого перечисления конкретных типов.

## Объекты анализа

- Загружаются backend-сборки корня `TicketBooking.*` (Api, Business-модули, BuildingBlocks, ServiceDefaults, AppHost, далее — Gateway/Worker/DatabaseMigrator по мере появления).
- Регистр бизнес-модулей вычисляется динамически из загруженных namespace: namespace вида `TicketBooking.<Module>.<Layer>`, где `Module` не входит в состав служебных проектов (`Api`, `Worker`, `DatabaseMigrator`, `Gateway`, `ServiceDefaults`, `BuildingBlocks`, `AppHost`).
- Слой `Contracts` — публичные application interfaces модуля (единственный разрешённый межмодульный видимый слой).
- Внутренние слои модуля — всё, что не `Contracts` (`Core`, далее `Domain`, `Application`, `Infrastructure`).

## Правила

### R1. Изоляция модулей (только через Contracts)

- Тип из любого слоя модуля A не должен зависеть от внутреннего слоя модуля B (A ≠ B).
- Единственный межмодульный видимый тип — тип из `TicketBooking.<B>.Contracts`.
- Запрещён доступ одного модуля к внутренним моделям/DbContext/repositories/Domain другого модуля на уровне типов.

### R2. Собственный Contracts

- Модуль может свободно использовать собственный `Contracts`; `Contracts` не зависит от внутренних слоёв того же модуля (`Core`/etc.).
- Внутренние слои модуля могут зависеть от собственного `Contracts`.

### R3. Чистота домена

Бизнес-модули (`TicketBooking.<Module>.*`) не должны зависеть от:

- ASP.NET Core (`Microsoft.AspNetCore.*`);
- Aspire (`Aspire.*`);
- YARP (`Yarp.*`, `Microsoft.ReverseProxy*`);
- PaymentEmulator (`PaymentEmulator.*`);
- frontend/React (`React.*`, `Microsoft.JSInterop.*`, browser/WebAssembly типов).

### R4. Direction composition roots

Бизнес-модули (`TicketBooking.<Module>.*`) не должны зависеть от composition roots:

- `TicketBooking.Api`;
- `TicketBooking.Worker`;
- `TicketBooking.DatabaseMigrator`;
- `TicketBooking.Gateway`.

### R5. Gateway не содержит бизнес-логики

- `TicketBooking.Gateway` не должен зависеть от внутренних слоёв бизнес-модулей (только от `...Contracts`), т.е. не содержит доменной/прикладной бизнес-логики (на project/namespace-уровне).

## Требования к реализации тестов

- Тесты используют `TngTech.ArchUnitNET.TUnit` + `TUnit`; тестовые методы — `[Test]`, правила выполняются через `.Check(Architecture)` / `architecture.CheckRule(rule)`.
- Правила задаются через namespace-префиксы (`ResideInNamespaceMatching`/`ResideInNamespace`) и регистр модулей, вычисленный по загруженным сборкам.
- Набор должен быть «зелёным» на текущем состоянии репозитория (Phase 0, каркас Companies) и автоматически распространяться на новые модули без правки тестов.
- Должен присутствовать по крайней мере один наблюдаемый негативный кейс, доказывающий способность правил обнаруживать нарушение (например, правило-самопроверка, намеренно подающее нарушителя в отдельном сценарии).
