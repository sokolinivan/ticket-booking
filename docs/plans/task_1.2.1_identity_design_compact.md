# Задача 1.2.1. Спроектировать БД пользователей системы

## Цель

Спроектировать хранение учетных записей пользователей системы для **модульного монолита на .NET**. Модуль должен обеспечивать пользователей, роли, права доступа, блокировки и интеграцию с аудитом.

Рекомендуемый модуль: `Identity`.

> В исходных требованиях указано, что учетные записи пользователей хранятся в БД пользователей системы, права определяют доступ к функционалу, действия пользователей логируются, а после трех неуспешных авторизаций пользователь блокируется на 30 минут.

---

## Задача 1.2.1.1. Определить границы Identity-модуля

Модуль отвечает за:

- пользователей системы;
- учетные данные;
- статусы и блокировки;
- роли;
- permissions;
- назначение ролей пользователям;
- предоставление информации о текущем пользователе другим модулям.

Модуль не отвечает за клиентов, мероприятия, заказы и платежи.

Другие модули не должны напрямую обращаться к таблицам или `DbSet` Identity-модуля.

---

## Задача 1.2.1.2. Спроектировать SystemUser

Основная сущность:

```text
SystemUser
```

Минимальные поля:

```text
Id
Login
NormalizedLogin
PasswordHash

FirstName
LastName
MiddleName
Email
Phone

Status

FailedLoginAttempts
LockedUntil
LastLoginAt

CreatedAt
CreatedBy
UpdatedAt
UpdatedBy

Version
```

Для идентификатора желательно использовать strongly typed ID:

```csharp
public readonly record struct SystemUserId(Guid Value);
```

---

## Задача 1.2.1.3. Спроектировать таблицу пользователей

SQL schema модуля:

```text
identity
```

Основная таблица:

```text
identity.SystemUsers
```

Ключевые требования:

- `Id` — PK;
- `NormalizedLogin` — unique;
- пароль хранится только как `PasswordHash`;
- пользователь не удаляется физически;
- состояние определяется через `Status`;
- `Version`/`rowversion` используется для optimistic concurrency.

Статусы, например:

```text
Active
Blocked
Disabled
Archived
```

---

## Задача 1.2.1.4. Реализовать правила авторизации и блокировки

Хранить:

```text
FailedLoginAttempts
LockedUntil
```

Алгоритм:

```text
неуспешный вход
    ↓
FailedLoginAttempts + 1
    ↓
3 попытки
    ↓
LockedUntil = Now + 30 минут
```

После успешной авторизации:

```text
FailedLoginAttempts = 0
LockedUntil = null
LastLoginAt = Now
```

---

## Задача 1.2.1.5. Спроектировать роли

Создать:

```text
identity.Roles
```

Поля:

```text
Id
Code
Name
Description
IsSystem
CreatedAt
UpdatedAt
```

Примеры ролей:

```text
ADMIN
SYSTEM_USER
CASHIER
```

Связь пользователя с ролями хранить отдельно:

```text
identity.SystemUserRoles
```

```text
SystemUserId
RoleId
AssignedAt
AssignedBy
```

Это позволяет назначать пользователю несколько ролей.

---

## Задача 1.2.1.6. Спроектировать permissions

Права следует отделить от ролей.

Создать:

```text
identity.Permissions
```

Примеры:

```text
users.read
users.manage

events.read
events.create
events.update
events.cancel

orders.read
orders.cancel

payments.refund

reports.read
settings.manage
```

Связь:

```text
identity.RolePermissions
```

Итоговая модель:

```text
SystemUser
    ↓
SystemUserRole
    ↓
Role
    ↓
RolePermission
    ↓
Permission
```

---

## Задача 1.2.1.7. Реализовать Persistence на EF Core

Identity должен иметь собственный:

```text
IdentityDbContext
```

Он управляет только объектами своего модуля:

```text
identity.SystemUsers
identity.Roles
identity.Permissions
identity.SystemUserRoles
identity.RolePermissions
```

Даже если весь модульный монолит использует одну физическую SQL Server БД, данные модулей логически разделяются SQL schemas.

Например:

```text
identity.*
events.*
orders.*
clients.*
payments.*
audit.*
```

---

## Задача 1.2.1.8. Сохранить границы модульного монолита

Другие модули не должны выполнять:

```csharp
identityDbContext.SystemUsers...
```

Для текущего пользователя предоставить контракт:

```csharp
public interface ICurrentUser
{
    SystemUserId Id { get; }
    IReadOnlySet<string> Permissions { get; }
}
```

Например, `Payments` проверяет:

```text
payments.refund
```

через контракт авторизации, а не через запрос к таблицам Identity.

Также желательно не создавать FK между таблицами разных модулей.

Например:

```text
events.Events.CreatedByUserId
```

хранит `SystemUserId`, но не обязан иметь физический FK на:

```text
identity.SystemUsers
```

---

## Задача 1.2.1.9. Реализовать аудит изменений

Identity хранит текущее состояние пользователя.

История действий должна передаваться в отдельный `Audit`-модуль.

Ключевые события:

```text
SystemUserCreated
SystemUserUpdated
SystemUserBlocked
SystemUserActivated

UserRoleAssigned
UserRoleRemoved

UserLoggedIn
UserLoginFailed
```

Взаимодействие:

```text
Identity
   ↓
Internal/Domain Event
   ↓
Audit
   ↓
audit.UserActions
```

Таким образом Identity не должен напрямую записывать данные через `AuditDbContext`.

---

## Задача 1.2.1.10. Настроить concurrency и историю изменений

Для защиты от параллельного редактирования использовать:

```text
rowversion
```

или:

```text
Version bigint
```

как EF Core concurrency token.

При конфликте изменений операция должна завершаться controlled concurrency error, а не перезаписывать чужие изменения.

Для аудита фиксировать:

```text
кто изменил;
когда изменил;
какую сущность;
какое поле;
старое значение;
новое значение.
```

---

## Задача 1.2.1.11. Создать EF Core migrations

Identity-модуль должен иметь собственные migrations.

Первая migration создает:

```text
identity.SystemUsers
identity.Roles
identity.Permissions
identity.SystemUserRoles
identity.RolePermissions
```

и:

- PK;
- FK внутри Identity-модуля;
- unique constraints;
- indexes;
- concurrency configuration.

Основные индексы:

```text
SystemUsers.NormalizedLogin UNIQUE
SystemUsers.Email
SystemUsers.Status

Roles.Code UNIQUE
Permissions.Code UNIQUE

SystemUserRoles(SystemUserId, RoleId) UNIQUE
RolePermissions(RoleId, PermissionId) UNIQUE
```

---

## Задача 1.2.1.12. Реализовать application use cases

Основные commands:

```text
CreateSystemUser
UpdateSystemUser

BlockSystemUser
UnblockSystemUser

AssignRole
RemoveRole

ChangePassword
```

Основные queries:

```text
GetSystemUser
GetSystemUsers
GetUserRoles
GetUserPermissions
```

---

## Задача 1.2.1.13. Реализовать первоначальную конфигурацию

Seed базовых ролей:

```text
ADMIN
SYSTEM_USER
CASHIER
```

Seed базовых permissions.

Предусмотреть bootstrap первого администратора через защищенную deployment-конфигурацию/secrets.

---

## Задача 1.2.1.14. Покрыть тестами

### Unit tests

Проверить:

- создание пользователя;
- изменение статуса;
- назначение/удаление роли;
- счетчик неуспешных авторизаций;
- блокировку после трех попыток;
- разблокировку.

### Integration tests

Проверить:

- сохранение через EF Core;
- уникальность login;
- roles и permissions;
- migrations;
- optimistic concurrency;
- корректность блокировки на 30 минут.

---

## Итоговая модель

```text
┌────────────────────────────┐
│ Identity Module            │
│                            │
│ SystemUser                 │
│ Role                       │
│ Permission                 │
│                            │
│ IdentityDbContext          │
│                            │
│ identity.SystemUsers       │
│ identity.Roles             │
│ identity.Permissions       │
│ identity.SystemUserRoles   │
│ identity.RolePermissions   │
└──────────────┬─────────────┘
               │
               │ Internal Events
               ↓
┌────────────────────────────┐
│ Audit Module               │
│ audit.UserActions          │
└────────────────────────────┘

Events / Orders / Payments / Reports
               │
               ↓
          ICurrentUser
               │
               ↓
            Identity
```

## Критерии готовности

Задача `1.2.1` считается выполненной, когда:

- определен агрегат `SystemUser`;
- определены роли и permissions;
- подготовлена ER-модель;
- определена SQL schema `identity`;
- реализован `IdentityDbContext`;
- подготовлены EF Core mappings и migrations;
- настроены indexes и constraints;
- реализована блокировка после трех неуспешных входов;
- настроен optimistic concurrency;
- реализованы основные commands и queries;
- определен `ICurrentUser` для других модулей;
- отсутствует прямой доступ других модулей к таблицам Identity;
- Identity передает значимые события в Audit;
- подготовлены unit и integration tests.
