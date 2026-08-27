using Microsoft.EntityFrameworkCore;
using TicketBooking.Identity.Domain;

namespace TicketBooking.Identity.Internal.Persistence;

internal sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    internal DbSet<SystemUser> SystemUsers => Set<SystemUser>();
    internal DbSet<Role> Roles => Set<Role>();
    internal DbSet<Permission> Permissions => Set<Permission>();
    internal DbSet<SystemUserRole> SystemUserRoles => Set<SystemUserRole>();
    internal DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
