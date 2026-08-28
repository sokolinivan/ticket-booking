# Task 10 Report

## Status

Implemented narrow translation of known PostgreSQL uniqueness violations into controlled Identity persistence conflicts for synchronous and asynchronous saves.

## TDD Evidence

- RED: `dotnet run --project tests/TicketBooking.IntegrationTests -- --treenode-filter "/*/*/IdentityUniquenessConflictTests/*" --minimum-expected-tests 1`
  - Result: build failed with `CS0246` for the missing `IdentityPersistenceConflict` and `IdentityPersistenceException` APIs.
- Initial GREEN run used real PostgreSQL and passed 5 of 7 tests. The two assignment tests exposed PostgreSQL reporting the composite primary-key names (`PK_SystemUserRoles` and `PK_RolePermissions`) before their redundant pair unique indexes.
- GREEN: added those two known assignment constraint names to the exact allowlist; all 7 real PostgreSQL tests then passed.

## Verification Evidence

- `dotnet build tests/TicketBooking.IntegrationTests/TicketBooking.IntegrationTests.csproj`
  - Result: succeeded with 0 warnings and 0 errors.
- `dotnet run --project tests/TicketBooking.IntegrationTests --no-build -- --treenode-filter "/*/*/IdentityUniquenessConflictTests/*" --minimum-expected-tests 1`
  - Result: 7 tests passed, 0 failed, 0 skipped.
- `git diff --check`
  - Result: no whitespace errors.

## Coverage

- Async save translation covers duplicate normalized login, role code, permission code, system-user role, and role permission conflicts.
- Sync save translation is exercised by a duplicate role-code conflict.
- Controlled exceptions retain the `PostgresException` as `InnerException` without incorporating provider text into the controlled message.
- A real PostgreSQL role primary-key `23505` remains the original `DbUpdateException`, proving unknown unique constraints are not mislabeled.

## Risks

- Tests require a reachable Docker daemon and access to the pinned `postgres:18.1-alpine` image.
- Assignment tables currently define both a composite primary key and a unique index over the same columns. PostgreSQL reports the primary-key constraint for duplicate pairs, so both exact known names are allowlisted for each assignment conflict.

## Review Fix Evidence

- RED: temporarily broadened the translator from SQLSTATE `23505` to every `PostgresException`, then ran `dotnet run --project tests/TicketBooking.IntegrationTests -- --treenode-filter "/*/*/IdentityUniquenessConflictTests/SaveChangesAsync_NonUniqueProviderFailure_PreservesDbUpdateException" --minimum-expected-tests 1`.
  - Result: the real PostgreSQL `23514` check violation was incorrectly wrapped as `IdentityPersistenceException`; the test failed expecting the original `DbUpdateException`.
- GREEN: restored the exact `23505` guard and reran the focused command.
  - Result: 1 test passed, 0 failed, 0 skipped.
- GREEN: `dotnet run --project tests/TicketBooking.IntegrationTests --no-build -- --treenode-filter "/*/*/IdentityUniquenessConflictTests/*" --minimum-expected-tests 1`.
  - Result: 8 tests passed, 0 failed, 0 skipped.
- `dotnet build tests/TicketBooking.IntegrationTests/TicketBooking.IntegrationTests.csproj --no-restore`
  - Result: succeeded with 0 warnings and 0 errors.
- `git diff --check`
  - Result: no whitespace errors.
