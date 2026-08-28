namespace TicketBooking.Identity.Domain;

public sealed class Role
{
    private readonly List<RolePermission> _permissions = [];

    private Role()
    {
    }

    public RoleId Id { get; internal set; }
    public string Code { get; internal set; } = null!;
    public string Name { get; internal set; } = null!;
    public long Version { get; internal set; }
    public IReadOnlyCollection<RolePermission> Permissions => _permissions;

    public static Role Create(RoleId id, string code, string name)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("Role ID cannot be empty.", nameof(id));
        }

        return new Role
        {
            Id = id,
            Code = Required(code, nameof(code)),
            Name = Required(name, nameof(name)),
            Version = 1,
        };
    }

    public RolePermission AddPermission(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        if (_permissions.Any(association => association.PermissionId == permission.Id))
        {
            throw new InvalidOperationException("The permission is already associated with this role.");
        }

        var association = RolePermission.Create(this, permission);
        _permissions.Add(association);
        permission.AddRole(association);
        return association;
    }

    private static string Required(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be empty or whitespace.", parameterName)
            : value;
}
