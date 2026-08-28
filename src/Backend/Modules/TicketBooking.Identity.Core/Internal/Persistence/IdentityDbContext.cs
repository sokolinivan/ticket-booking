using Microsoft.EntityFrameworkCore;
using Npgsql;
using TicketBooking.Identity.Domain;

namespace TicketBooking.Identity.Internal.Persistence;

internal sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    internal DbSet<SystemUser> SystemUsers => Set<SystemUser>();
    internal DbSet<Role> Roles => Set<Role>();
    internal DbSet<Permission> Permissions => Set<Permission>();
    internal DbSet<SystemUserRole> SystemUserRoles => Set<SystemUserRole>();
    internal DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        try
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        catch (DbUpdateException exception) when (Translate(exception) is { } translated)
        {
            throw translated;
        }
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateException exception) when (Translate(exception) is { } translated)
        {
            throw translated;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }

    private static IdentityPersistenceException? Translate(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres)
        {
            return null;
        }

        var conflict = postgres.ConstraintName switch
        {
            IdentityConstraintNames.SystemUsersNormalizedLoginUniqueIndex => IdentityPersistenceConflict.DuplicateNormalizedLogin,
            IdentityConstraintNames.RolesCodeUniqueIndex => IdentityPersistenceConflict.DuplicateRoleCode,
            IdentityConstraintNames.PermissionsCodeUniqueIndex => IdentityPersistenceConflict.DuplicatePermissionCode,
            IdentityConstraintNames.SystemUserRolesPairUniqueIndex or
                IdentityConstraintNames.SystemUserRolesPrimaryKey => IdentityPersistenceConflict.DuplicateSystemUserRole,
            IdentityConstraintNames.RolePermissionsPairUniqueIndex or
                IdentityConstraintNames.RolePermissionsPrimaryKey => IdentityPersistenceConflict.DuplicateRolePermission,
            _ => (IdentityPersistenceConflict?)null,
        };

        return conflict is { } knownConflict
            ? new IdentityPersistenceException(knownConflict, postgres)
            : null;
    }
}
