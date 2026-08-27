# Outcome

В `TicketBooking.ArchitectureTests` создан исполняемый набор архитектурных тестов (ArchUnitNET + TUnit), который автоматически проверяет недопустимые project/namespace dependencies согласно правилам модульного монолита из `docs/plan.md`. Тесты построены через namespace-префиксы и начинают проверять ограничения по мере добавления модулей, без доработки самих тестов.

# Scope

## Источник требований: `docs/plan.md`

Пользователь предоставил `docs/plan.md` как источник требований (`@docs/plan.md`) и поручил взять задачу «Добавить architecture tests, запрещающие недопустимые project/namespace dependencies». Включено полное покрытие исходного файла.

## Source coverage

Ниже — карта покрытия релевантных исполнительных единиц `docs/plan.md`.

| Источник (file:line) | Единица | Статус чтения | Семантика | Spec | ID | Покрытие |
|---|---|---|---|---|---|---|
| plan.md:41 | «Добавить architecture tests, запрещающие недопустимые project/namespace dependencies» | complete | Основная задача: создать архитектурные тесты, запрещающие недопустимые зависимости проектов/namespace | specs/architecture-tests/spec.md | A1–A5 | covered |
| plan.md:31–35 | Существование проектов тестов (в т.ч. `TicketBooking.ArchitectureTests`) | complete | Контекст: тестовый контур присутствует | — | — | background |
| plan.md:40 | «Зафиксировать правила зависимостей между модулями» | complete | Предшествующее/смежное требование — зафиксировать правила | specs/architecture-tests/spec.md (инварианты) | A1 | covered |
| plan.md:119–121 | «Запретить прямой доступ одного модуля к таблицам другого», «Определить публичные application interfaces модулей» | complete | Изоляция модулей: ссылки между модулями только через публичные Contracts | specs/architecture-tests/spec.md | A2 | covered |
| plan.md:122–123 | «Вынести общие примитивы в BuildingBlocks…», «Не допускать появления общей бизнес-модели между модулями» | complete | Модули не должны зависеть от чужих внутренних слоёв/моделей | specs/architecture-tests/spec.md | A2 | covered |
| plan.md:125–129 | «Добавить architecture tests для запрета зависимостей бизнес-модулей от: Aspire; YARP; React/frontend; PaymentEmulator» | complete | Чистота домена: бизнес-модули не зависят от фреймворков и эмулятора | specs/architecture-tests/spec.md | A3 | covered |
| plan.md:124 | «Настроить регистрацию модулей в Api, Worker, DatabaseMigrator как composition roots» | complete | Направление зависимостей: модули не ссылаются на composition roots | specs/architecture-tests/spec.md | A4 | covered |
| plan.md:500–505 | Фаза 13 «Architecture»: запрет ссылок между внутренними слоями разных модулей; запрет прямого доступа к чужим DbContext/repositories; запрет зависимостей домена от ASP.NET Core/Aspire/YARP; запрет business logic в Gateway | complete | Расширенный набор правил (в рамках данной задачи — namespace/project-уровень) | specs/architecture-tests/spec.md | A2, A3, A4, A5 | covered |

Все единицы исходного файла прочитаны полностью (`complete`). Исполнительные единицы, попадающие в объём задачи, покрыты целевой спецификацией и acceptance-критериями. Дополнительных недоступных или частично прочитанных источников нет.

## Объём работ

1. Настроить загрузку backend-сборок (`TicketBooking.*`) в `TicketBooking.ArchitectureTests` через ArchUnitNET.
2. Реализовать набор namespace-правил зависимостей (см. спецификацию) с регистром модулей, определяемым по загруженным namespace, чтобы правила автоматически распространялись на будущие модули.
3. Убедиться, что правило может фактически срабатывать (наличие по крайней мере одного наблюдаемого отрицательного кейса в тестах).

# Non-goals

- Не создавать новые бизнес-модули, Gateway, Worker, DatabaseMigrator или их проекты (это отдельные задачи плана).
- Не реализовывать правила на уровне БД (прямой доступ к чужим таблицам/схемам, миграции) — это относится к Phase 2/13 и выходит за рамки project/namespace-уровня.
- Не добавлять GitHub Actions workflow (отдельная задача plan.md:42).
- Не менять `Directory.Build.props`, анализеры или требования warnings-as-errors (отдельная задача).
- Не обновлять docker-compose: root docker-compose на данный момент отсутствует, а в этом изменении AppHost и settings-переменные не меняются.
- Не трогать существующие тестовые проекты (UnitTests, IntegrationTests, SystemTests) и их поведение.

# Acceptance examples

- **A1.** Правила зависимостей модулей зафиксированы как исполняемые архитектурные тесты (ArchUnitNET), а не только документация.
- **A2.** Изоляция модулей: тип из внутреннего слоя модуля A не может зависеть от внутреннего слоя/модели модуля B; единственный разрешённый межмодульный видимый тип — публичный `Contracts` другого модуля.
- **A3.** Чистота домена: бизнес-модули не зависят от ASP.NET Core, Aspire, YARP, PaymentEmulator и frontend/React.
- **A4.** Direction composition roots: бизнес-модули не зависят от `TicketBooking.Api`, `TicketBooking.Worker`, `TicketBooking.DatabaseMigrator`, `TicketBooking.Gateway`.
- **A5.** Набор тестов собирается и зелёный на текущем состоянии репозитория (`dotnet test` ArchitectureTests), правила сформулированы через namespace-префиксы и автоматически применяются к новым модулям.

# Constraints and invariants

- Тесты используют `TngTech.ArchUnitNET.TUnit` + `TUnit` (уже подключены в `TicketBooking.ArchitectureTests.csproj`).
- Правила выражены через namespace-префиксы (`TicketBooking.<Module>.<Layer>`), а не перечисление конкретных типов, чтобы не устаревать при добавлении модулей.
- Соглашение namespace: корень `TicketBooking.*`; слои модуля — `Contracts` (публичные интерфейсы) и внутренние слои (`Core`, далее `Domain`/`Application`/`Infrastructure`); composition roots: `Api`, `Worker`, `DatabaseMigrator`, `Gateway`; служебные: `ServiceDefaults`, `BuildingBlocks`, `AppHost`.
- Межмодульные ссылки разрешены только на `...Contracts` другого модуля; собственный `Contracts` модуль может использовать сам.
- `CodeAnalysisTreatWarningsAsErrors` и `TreatWarningsAsErrors` включены (Directory.Build.props) — код тестов не должен давать предупреждений.

# Decisions

- **D1 (пользователь, Пакет A):** Полный набор правил по плану: изоляция модулей, направление слоёв Contracts↔Core, чистота домена (ASP.NET Core/Aspire/YARP/PaymentEmulator/frontend), запрет ссылок на composition roots, запрет business logic в Gateway (на project/namespace-уровне).
- **D2:** Тестовый framework — ArchUnitNET (TUnit-расширение), уже подключённый в csproj.
- **D3:** Регистр модулей вычисляется по загруженным сборкам/namespace, а не фиксируется вручную; правила применяются автоматически к будущим модулям.
- **D4:** Правила направлены на project/namespace-уровень (class-level dependency analysis через ArchUnitNET); правила уровня БД (таблицы/схемы) не входят в задачу.

# Open questions

Нет нерешённых вопросов. Пользователь подтвердил итоговое согласование (Пакет A).

# Verification expectations

- `dotnet build TicketBooking.slnx` — успешно, без предупреждений.
- `dotnet test tests/TicketBooking.ArchitectureTests` — все тесты проходят (зелёные) на текущем состоянии репозитория.
- Наличие наблюдаемого негативного кейса в тестах, подтверждающего, что правила способны обнаруживать нарушение (например, правило-самопроверка против временной сборки-нарушителя, или проверка, что `Core` не зависит от `Contracts` другого модуля).
