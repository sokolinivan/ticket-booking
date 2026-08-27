namespace TicketBooking.Identity.Domain;

public sealed class SystemUser
{
    private int _failedLoginAttempts;

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

    private static string Required(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be empty or whitespace.", parameterName)
            : value;
}
