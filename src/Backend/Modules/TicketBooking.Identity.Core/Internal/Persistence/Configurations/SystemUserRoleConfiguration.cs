using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketBooking.Identity.Domain;

namespace TicketBooking.Identity.Internal.Persistence.Configurations;

internal sealed class SystemUserRoleConfiguration : IEntityTypeConfiguration<SystemUserRole>
{
    public void Configure(EntityTypeBuilder<SystemUserRole> builder)
    {
        builder.ToTable("SystemUserRoles", "identity");
        builder.HasKey(assignment => new { assignment.SystemUserId, assignment.RoleId })
            .HasName(IdentityConstraintNames.SystemUserRolesPrimaryKey);
        builder.Property(assignment => assignment.SystemUserId).HasConversion<SystemUserIdConverter, SystemUserIdComparer>().HasColumnName("SystemUserId").HasColumnType("uuid").ValueGeneratedNever();
        builder.Property(assignment => assignment.RoleId).HasConversion<RoleIdConverter, RoleIdComparer>().HasColumnName("RoleId").HasColumnType("uuid").ValueGeneratedNever();
        builder.Property(assignment => assignment.AssignedAt).HasColumnName("AssignedAt").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(assignment => assignment.AssignedBy).HasColumnName("AssignedBy").HasColumnType("character varying(200)").HasMaxLength(200).IsRequired();
        builder.HasIndex(assignment => new { assignment.SystemUserId, assignment.RoleId })
            .IsUnique()
            .HasDatabaseName(IdentityConstraintNames.SystemUserRolesPairUniqueIndex);

        builder.HasOne(assignment => assignment.Role)
            .WithMany()
            .HasForeignKey(assignment => assignment.RoleId)
            .HasConstraintName(IdentityConstraintNames.SystemUserRolesRoleForeignKey)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
