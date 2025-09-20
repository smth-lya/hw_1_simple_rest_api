using HW1.Api.Domain.Models;

namespace HW1.Api.Application.DTOs;

public sealed record UserDto(Guid Id, string Username)
{
    public DateTime? CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public static UserDto FromUser(User user) => new(user.Id, user.Username)
    {
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };
}