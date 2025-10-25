using HW1.Api.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HW1.Api.Infrastructure.Database.Configurations;

public class TelegramUserConfiguration : IEntityTypeConfiguration<TelegramUser>
{
    public void Configure(EntityTypeBuilder<TelegramUser> builder)
    {
        builder.ToTable("telegram_users");

        builder.HasKey(t => t.TelegramUserId);
        
        builder.Property(t => t.TelegramUserId)
            .HasColumnName("telegram_user_id")
            .ValueGeneratedNever(); 
        
        builder.Property(t => t.ChatId)
            .HasColumnName("chat_id")
            .IsRequired();

        builder.Property(t => t.Username)
            .HasColumnName("username")
            .HasMaxLength(100)
            .IsRequired(false); 

        builder.Property(t => t.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .IsRequired(false); 

        builder.Property(t => t.RegisteredAt)
            .HasColumnName("registered_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(t => t.LastActivity)
            .HasColumnName("last_activity")
            .HasDefaultValueSql("NOW()");

        builder.Property(t => t.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(t => t.SystemUserId)
            .HasColumnName("system_user_id")
            .IsRequired(false); 
        
        // Внешний ключ к системным пользователям
        builder.HasOne(t => t.SystemUser)
            .WithMany()
            .HasForeignKey(t => t.SystemUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Индексы
        builder.HasIndex(t => t.TelegramUserId)
            .HasDatabaseName("ix_telegram_users_telegram_user_id")
            .IsUnique();

        builder.HasIndex(t => t.ChatId)
            .HasDatabaseName("ix_telegram_users_chat_id");

        builder.HasIndex(t => t.Username)
            .HasDatabaseName("ix_telegram_users_username");

        builder.HasIndex(t => t.IsActive)
            .HasDatabaseName("ix_telegram_users_is_active");
    }
}