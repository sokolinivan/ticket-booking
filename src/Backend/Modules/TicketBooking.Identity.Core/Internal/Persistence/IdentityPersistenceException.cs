namespace TicketBooking.Identity.Internal.Persistence;

internal sealed class IdentityPersistenceException(
    IdentityPersistenceConflict conflict,
    Exception innerException)
    : Exception($"Identity persistence conflict: {conflict}.", innerException)
{
    internal IdentityPersistenceConflict Conflict { get; } = conflict;
}
