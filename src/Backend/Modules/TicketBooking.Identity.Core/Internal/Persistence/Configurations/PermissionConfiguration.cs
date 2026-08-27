using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketBooking.Identity.Domain;

namespace TicketBooking.Identity.Internal.Persistence.Configurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions", "identity");
        builder.HasKey(permission => permission.Id);
        builder.Property(permission => permission.Id).HasConversion<PermissionIdConverter, PermissionIdComparer>().HasColumnName("Id").HasColumnType("uuid").ValueGeneratedNever();
        builder.Property(permission => permission.Code).HasColumnName("Code").HasColumnType("character varying(128)").HasMaxLength(128).IsRequired();
        builder.Property(permission => permission.Name).HasColumnName("Name").HasColumnType("character varying(256)").HasMaxLength(256).IsRequired();
        builder.Property(permission => permission.Version).HasColumnName("Version").HasColumnType("bigint").HasDefaultValue(1L).ValueGeneratedNever().IsConcurrencyToken().IsRequired();

        builder.HasMany(permission => permission.Roles)
            .WithOne(association => association.Permission)
            .HasForeignKey(association => association.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(permission => permission.Roles).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
