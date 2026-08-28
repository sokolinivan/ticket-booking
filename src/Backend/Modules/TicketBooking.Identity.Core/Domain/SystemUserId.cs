namespace TicketBooking.Identity.Domain;

public readonly record struct SystemUserId(Guid Value)
{
    public static SystemUserId New() => new(Guid.NewGuid());
}
