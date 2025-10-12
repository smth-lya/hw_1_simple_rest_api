namespace HW1.Api.Domain.Models;

public class LoginAttempt
{
    public int AttemptId { get; set; }
    public Guid UserId { get; set; }
    public DateTime AttemptTime { get; set; } = DateTime.UtcNow;
    public bool IsSuccessful { get; set; }
    
    public string? IpAddress { get; set; }
    
    public string? UserAgent { get; set; }

    public virtual User User { get; set; } = null!;
}