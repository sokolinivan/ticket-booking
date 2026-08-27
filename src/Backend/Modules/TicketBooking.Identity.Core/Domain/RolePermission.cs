namespace TicketBooking.Identity.Domain;

#pragma warning disable CA1711 // RolePermission is the explicit domain association required by the model.
public sealed class RolePermission
{
    private RolePermission()
    {
    }

    public RoleId RoleId { get; internal set; }
    public PermissionId PermissionId { get; internal set; }
    public Role Role { get; internal set; } = null!;
    public Permission Permission { get; internal set; } = null!;

    internal static RolePermission Create(Role role, Permission permission) => new()
    {
        RoleId = role.Id,
        PermissionId = permission.Id,
        Role = role,
        Permission = permission,
    };
}
#pragma warning restore CA1711
