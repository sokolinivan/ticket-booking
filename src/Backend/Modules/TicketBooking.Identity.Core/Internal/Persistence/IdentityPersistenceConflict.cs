namespace TicketBooking.Identity.Internal.Persistence;

internal enum IdentityPersistenceConflict
{
    DuplicateNormalizedLogin,
    DuplicateRoleCode,
    DuplicatePermissionCode,
    DuplicateSystemUserRole,
    DuplicateRolePermission,
    Concurrency,
}
