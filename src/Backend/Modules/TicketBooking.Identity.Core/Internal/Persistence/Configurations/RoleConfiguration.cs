using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketBooking.Identity.Domain;

namespace TicketBooking.Identity.Internal.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "identity");
        builder.HasKey(role => role.Id);
        builder.Property(role => role.Id).HasConversion<RoleIdConverter, RoleIdComparer>().HasColumnName("Id").HasColumnType("uuid").ValueGeneratedNever();
        builder.Property(role => role.Code).HasColumnName("Code").HasColumnType("character varying(128)").HasMaxLength(128).IsRequired();
        builder.Property(role => role.Name).HasColumnName("Name").HasColumnType("character varying(256)").HasMaxLength(256).IsRequired();
        builder.Property(role => role.Version).HasColumnName("Version").HasColumnType("bigint").HasDefaultValue(1L).ValueGeneratedNever().IsConcurrencyToken().IsRequired();

        builder.HasMany(role => role.Permissions)
            .WithOne(association => association.Role)
            .HasForeignKey(association => association.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(role => role.Permissions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
