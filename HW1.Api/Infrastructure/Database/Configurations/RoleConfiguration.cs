using HW1.Api.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HW1.Api.Infrastructure.Database.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.RoleId);

        builder.Property(r => r.RoleId)
            .HasColumnName("role_id")
            .ValueGeneratedOnAdd();

        builder.Property(r => r.RoleName)
            .HasColumnName("role_name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasMaxLength(255);

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(r => r.RoleName)
            .HasDatabaseName("ix_roles_role_name")
            .IsUnique();

        // Seed
        builder.HasData(
            new Role { 
                RoleId = 1, 
                RoleName = "Admin", 
                Description = "Administrator with full access",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Role { 
                RoleId = 2, 
                RoleName = "User", 
                Description = "Regular user",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Role { 
                RoleId = 3, 
                RoleName = "Moderator", 
                Description = "Content moderator",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Role { 
                RoleId = 4, 
                RoleName = "VIP", 
                Description = "VIP user with additional privileges",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}