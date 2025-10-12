using System.Diagnostics.CodeAnalysis;

namespace HW1.Api.Domain.Models;

public class User
{
    public User()
    { }

    [SetsRequiredMembers]
    public User(string username, string passwordHash)
    {
        Username = username;
        PasswordHash = passwordHash;
    }
    
    public Guid UserId { get; init; } = Guid.NewGuid();
    public required string Username { get; init; }
    public required string PasswordHash { get; init; }
    public Gender? Gender { get; init; }
    
    public DateOnly? DateOfBirth { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual UserProfile? UserProfile { get; set; }
    public virtual ICollection<LoginAttempt> LoginAttempts { get; set; } = new List<LoginAttempt>();
}

public enum Gender
{
    Male,
    Female
}