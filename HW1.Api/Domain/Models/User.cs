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
}