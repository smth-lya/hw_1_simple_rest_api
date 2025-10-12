namespace HW1.Api.Domain.Models;

public class UserProfile
{
    public int ProfileId { get; set; }
    public Guid UserId { get; set; }
    
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    
    public string? PhoneNumber { get; set; }

    public string? Country { get; set; }
    public string? City { get; set; }
    
    public string? AvatarUrl { get; set; }

    public virtual User User { get; set; } = null!;
}