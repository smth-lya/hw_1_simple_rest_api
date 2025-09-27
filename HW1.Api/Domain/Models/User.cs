using System.Diagnostics.CodeAnalysis;

namespace HW1.Api.Domain.Models;

public sealed class User
{
    public User()
    { }

    [SetsRequiredMembers]
    public User(string username, string passwordHash)
    {
        Username = username;
        PasswordHash = passwordHash;
    }
    
    public Guid Id { get; init; }
    public required string Username { get; init; }
    public required string PasswordHash { get; init; }
    public Gender? Gender { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

public enum Gender
{
    Male,
    Female
}