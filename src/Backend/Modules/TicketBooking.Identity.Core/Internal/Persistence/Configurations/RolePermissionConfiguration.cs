using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketBooking.Identity.Domain;

namespace TicketBooking.Identity.Internal.Persistence.Configurations;

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions", "identity");
        builder.HasKey(association => new { association.RoleId, association.PermissionId });
        builder.Property(association => association.RoleId).HasConversion<RoleIdConverter, RoleIdComparer>().HasColumnName("RoleId").HasColumnType("uuid").ValueGeneratedNever();
        builder.Property(association => association.PermissionId).HasConversion<PermissionIdConverter, PermissionIdComparer>().HasColumnName("PermissionId").HasColumnType("uuid").ValueGeneratedNever();
    }
}
