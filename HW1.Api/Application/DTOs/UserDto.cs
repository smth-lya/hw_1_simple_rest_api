using HW1.Api.Domain.Models;

namespace HW1.Api.Application.DTOs;

public sealed record UserDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = default!;
    public Gender? Gender { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }

    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();

    public static UserDto FromUser(User user) => new()
    {
        Id = user.UserId,
        Username = user.Username,
        Gender = user.Gender,
        DateOfBirth = user.DateOfBirth,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
        LastLoginAt = user.LastLoginAt,
        Roles = user.UserRoles
            .Select(ur => ur.Role.RoleName)
            .ToArray()
    };
}