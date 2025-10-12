using HW1.Api.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HW1.Api.Infrastructure.Database.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");

        builder.HasKey(up => up.ProfileId);
        
        builder.Property(up => up.ProfileId)
            .HasColumnName("profile_id")
            .ValueGeneratedOnAdd();

        builder.Property(up => up.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(up => up.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(50);

        builder.Property(up => up.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(50);

        builder.Property(up => up.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(20);

        builder.Property(up => up.Country)
            .HasColumnName("country")
            .HasMaxLength(50);

        builder.Property(up => up.City)
            .HasColumnName("city")
            .HasMaxLength(50);

        builder.Property(up => up.AvatarUrl)
            .HasColumnName("avatar_url")
            .HasMaxLength(255);

        builder.HasIndex(up => up.UserId)
            .HasDatabaseName("ix_user_profiles_user_id")
            .IsUnique();

        builder.HasOne(up => up.User)
            .WithOne(u => u.UserProfile)
            .HasForeignKey<UserProfile>(up => up.UserId)
            .HasConstraintName("fk_user_profiles_users");
    }
}