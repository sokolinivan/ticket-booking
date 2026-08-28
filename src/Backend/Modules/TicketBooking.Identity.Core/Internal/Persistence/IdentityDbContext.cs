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
        AdvanceVersions();

        try
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new IdentityPersistenceException(IdentityPersistenceConflict.Concurrency, exception);
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
        AdvanceVersions();

        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new IdentityPersistenceException(IdentityPersistenceConflict.Concurrency, exception);
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

    private void AdvanceVersions()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Modified
                || entry.Entity is not (SystemUser or Role or Permission))
            {
                continue;
            }

            var version = entry.Property(nameof(SystemUser.Version));
            var originalVersion = (long)version.OriginalValue!;
            version.CurrentValue = checked(originalVersion + 1);
        }
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
