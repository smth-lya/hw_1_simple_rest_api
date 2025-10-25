namespace HW1.Api.Domain.Models;

public class TelegramUser
{
    public long TelegramUserId { get; set; }
    public long ChatId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public Guid? SystemUserId { get; set; }
    
    public virtual User? SystemUser { get; set; }
}