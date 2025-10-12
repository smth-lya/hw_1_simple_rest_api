using HW1.Api.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HW1.Api.Infrastructure.Database.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        builder.HasKey(ur => ur.UserRoleId);

        builder.Property(ur => ur.UserRoleId)
            .HasColumnName("user_role_id")
            .ValueGeneratedOnAdd();

        builder.Property(ur => ur.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(ur => ur.RoleId)
            .HasColumnName("role_id")
            .IsRequired();

        builder.Property(ur => ur.AssignedAt)
            .HasColumnName("assigned_at")
            .HasDefaultValueSql("NOW()");
        
        builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
            .HasDatabaseName("ix_user_roles_user_id_role_id")
            .IsUnique();

        builder.HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .HasConstraintName("fk_user_roles_users");

        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .HasConstraintName("fk_user_roles_roles");
    }
}