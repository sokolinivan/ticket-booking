namespace TicketBooking.Identity.Domain;

#pragma warning disable CA1711 // Permission is the domain term required by the authorization model.
public sealed class Permission
{
    private readonly List<RolePermission> _roles = [];

    private Permission()
    {
    }

    public PermissionId Id { get; internal set; }
    public string Code { get; internal set; } = null!;
    public string Name { get; internal set; } = null!;
    public long Version { get; internal set; }
    public IReadOnlyCollection<RolePermission> Roles => _roles;

    public static Permission Create(PermissionId id, string code, string name)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("Permission ID cannot be empty.", nameof(id));
        }

        return new Permission
        {
            Id = id,
            Code = Required(code, nameof(code)),
            Name = Required(name, nameof(name)),
            Version = 1,
        };
    }

    internal void AddRole(RolePermission association) => _roles.Add(association);

    private static string Required(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be empty or whitespace.", parameterName)
            : value;
}
#pragma warning restore CA1711
