namespace TicketBooking.Identity.Domain;

public readonly record struct PermissionId(Guid Value)
{
    public static PermissionId New() => new(Guid.NewGuid());
}
