namespace TicketBooking.Identity.Domain;

public sealed class SystemUser
{
    private int _failedLoginAttempts;
    private readonly List<SystemUserRole> _roles = [];

    private SystemUser()
    {
    }

    public SystemUserId Id { get; internal set; }
    public string Login { get; internal set; } = null!;
    public string NormalizedLogin { get; internal set; } = null!;
    public string PasswordHash { get; internal set; } = null!;
    public string FirstName { get; internal set; } = null!;
    public string LastName { get; internal set; } = null!;
    public string Email { get; internal set; } = null!;
    public string? PhoneNumber { get; internal set; }
    public SystemUserStatus Status { get; internal set; }
    public DateTimeOffset? LastLoginAt { get; internal set; }

    public int FailedLoginAttempts
    {
        get => _failedLoginAttempts;
        internal set => _failedLoginAttempts = value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value));
    }

    public DateTimeOffset CreatedAt { get; internal set; }
    public string CreatedBy { get; internal set; } = null!;
    public DateTimeOffset? UpdatedAt { get; internal set; }
    public string? UpdatedBy { get; internal set; }
    public long Version { get; internal set; }
    public IReadOnlyCollection<SystemUserRole> Roles => _roles;

    public static SystemUser Create(
        SystemUserId id,
        string login,
        string normalizedLogin,
        string passwordHash,
        string firstName,
        string lastName,
        string email,
        string? phoneNumber,
        DateTimeOffset createdAt,
        string createdBy)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("System user ID cannot be empty.", nameof(id));
        }

        return new SystemUser
        {
            Id = id,
            Login = Required(login, nameof(login)),
            NormalizedLogin = Required(normalizedLogin, nameof(normalizedLogin)),
            PasswordHash = Required(passwordHash, nameof(passwordHash)),
            FirstName = Required(firstName, nameof(firstName)),
            LastName = Required(lastName, nameof(lastName)),
            Email = Required(email, nameof(email)),
            PhoneNumber = phoneNumber,
            Status = SystemUserStatus.Active,
            CreatedAt = createdAt,
            CreatedBy = Required(createdBy, nameof(createdBy)),
            Version = 1,
        };
    }

    public SystemUserRole AssignRole(Role role, DateTimeOffset assignedAt, string assignedBy)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (_roles.Any(assignment => assignment.RoleId == role.Id))
        {
            throw new InvalidOperationException("The role is already assigned to this system user.");
        }

        if (string.IsNullOrWhiteSpace(assignedBy))
        {
            throw new ArgumentException("Value cannot be empty or whitespace.", nameof(assignedBy));
        }

        var assignment = SystemUserRole.Create(this, role, assignedAt, assignedBy);
        _roles.Add(assignment);
        return assignment;
    }

    public void Block(DateTimeOffset changedAt, string changedBy) =>
        TransitionTo(SystemUserStatus.Blocked, changedAt, changedBy);

    public void Disable(DateTimeOffset changedAt, string changedBy) =>
        TransitionTo(SystemUserStatus.Disabled, changedAt, changedBy);

    public void Activate(DateTimeOffset changedAt, string changedBy) =>
        TransitionTo(SystemUserStatus.Active, changedAt, changedBy);

    public void Archive(DateTimeOffset changedAt, string changedBy) =>
        TransitionTo(SystemUserStatus.Archived, changedAt, changedBy);

    private void TransitionTo(SystemUserStatus target, DateTimeOffset changedAt, string changedBy)
    {
        if (Status == SystemUserStatus.Archived)
        {
            throw new InvalidOperationException("An archived system user cannot change status.");
        }

        var allowed = target == SystemUserStatus.Archived
            || Status == SystemUserStatus.Active && target is SystemUserStatus.Blocked or SystemUserStatus.Disabled
            || Status == SystemUserStatus.Blocked && target is SystemUserStatus.Active or SystemUserStatus.Disabled
            || Status == SystemUserStatus.Disabled && target == SystemUserStatus.Active;

        if (!allowed)
        {
            throw new InvalidOperationException($"Cannot transition system user from {Status} to {target}.");
        }

        ArgumentOutOfRangeException.ThrowIfEqual(changedAt, default, nameof(changedAt));
        var actor = Required(changedBy, nameof(changedBy));

        Status = target;
        UpdatedAt = changedAt;
        UpdatedBy = actor;
    }

    private static string Required(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be empty or whitespace.", parameterName)
            : value;
}
