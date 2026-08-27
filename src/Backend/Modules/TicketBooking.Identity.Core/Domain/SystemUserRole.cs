namespace TicketBooking.Identity.Domain;

public sealed class SystemUserRole
{
    private SystemUserRole()
    {
    }

    public SystemUserId SystemUserId { get; internal set; }
    public RoleId RoleId { get; internal set; }
    public DateTimeOffset AssignedAt { get; internal set; }
    public string AssignedBy { get; internal set; } = null!;
    public SystemUser SystemUser { get; internal set; } = null!;
    public Role Role { get; internal set; } = null!;

    internal static SystemUserRole Create(
        SystemUser systemUser,
        Role role,
        DateTimeOffset assignedAt,
        string assignedBy) => new()
        {
            SystemUserId = systemUser.Id,
            RoleId = role.Id,
            AssignedAt = assignedAt,
            AssignedBy = assignedBy,
            SystemUser = systemUser,
            Role = role,
        };
}
