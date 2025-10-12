using HW1.Api.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HW1.Api.Infrastructure.Database.Configurations;

public class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        builder.ToTable("login_attempts");

        builder.HasKey(la => la.AttemptId);
        
        builder.Property(la => la.AttemptId)
            .HasColumnName("attempt_id")
            .ValueGeneratedOnAdd();

        builder.Property(la => la.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(la => la.AttemptTime)
            .HasColumnName("attempt_time")
            .HasDefaultValueSql("NOW()");

        builder.Property(la => la.IsSuccessful)
            .HasColumnName("is_successful")
            .IsRequired();

        builder.Property(la => la.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45); 

        builder.Property(la => la.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(500);

        builder.HasIndex(la => la.UserId)
            .HasDatabaseName("ix_login_attempts_user_id");

        builder.HasIndex(la => la.AttemptTime)
            .HasDatabaseName("ix_login_attempts_attempt_time");

        builder.HasOne(la => la.User)
            .WithMany(u => u.LoginAttempts)
            .HasForeignKey(la => la.UserId)
            .HasConstraintName("fk_login_attempts_users");
    }
}