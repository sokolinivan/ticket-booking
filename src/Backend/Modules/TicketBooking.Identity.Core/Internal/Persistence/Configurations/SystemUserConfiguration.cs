using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketBooking.Identity.Domain;

namespace TicketBooking.Identity.Internal.Persistence.Configurations;

internal sealed class SystemUserConfiguration : IEntityTypeConfiguration<SystemUser>
{
    public void Configure(EntityTypeBuilder<SystemUser> builder)
    {
        builder.ToTable("SystemUsers", "identity");
        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasConversion<SystemUserIdConverter, SystemUserIdComparer>()
            .HasColumnName("Id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();
        builder.Property(user => user.Login).HasColumnName("Login").HasColumnType("character varying(256)").HasMaxLength(256).IsRequired();
        builder.Property(user => user.NormalizedLogin).HasColumnName("NormalizedLogin").HasColumnType("character varying(256)").HasMaxLength(256).IsRequired();
        builder.Property(user => user.PasswordHash).HasColumnName("PasswordHash").HasColumnType("character varying(1024)").HasMaxLength(1024).IsRequired();
        builder.Property(user => user.FirstName).HasColumnName("FirstName").HasColumnType("character varying(200)").HasMaxLength(200).IsRequired();
        builder.Property(user => user.LastName).HasColumnName("LastName").HasColumnType("character varying(200)").HasMaxLength(200).IsRequired();
        builder.Property(user => user.Email).HasColumnName("Email").HasColumnType("character varying(320)").HasMaxLength(320).IsRequired();
        builder.Property(user => user.PhoneNumber).HasColumnName("PhoneNumber").HasColumnType("character varying(64)").HasMaxLength(64).IsRequired(false);
        builder.Property(user => user.Status).HasConversion<string>().HasColumnName("Status").HasColumnType("character varying(32)").HasMaxLength(32).IsRequired();
        builder.Property(user => user.LastLoginAt).HasColumnName("LastLoginAt").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(user => user.FailedLoginAttempts).HasColumnName("FailedLoginAttempts").HasColumnType("integer").IsRequired();
        builder.Property(user => user.CreatedAt).HasColumnName("CreatedAt").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(user => user.CreatedBy).HasColumnName("CreatedBy").HasColumnType("character varying(200)").HasMaxLength(200).IsRequired();
        builder.Property(user => user.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(user => user.UpdatedBy).HasColumnName("UpdatedBy").HasColumnType("character varying(200)").HasMaxLength(200).IsRequired(false);
        builder.Property(user => user.Version)
            .HasColumnName("Version")
            .HasColumnType("bigint")
            .HasDefaultValue(1L)
            .ValueGeneratedNever()
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasMany(user => user.Roles)
            .WithOne(assignment => assignment.SystemUser)
            .HasForeignKey(assignment => assignment.SystemUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(user => user.Roles).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
