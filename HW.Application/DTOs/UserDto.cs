namespace HW.Application.DTOs;

public sealed record UserDto(Guid Id, string Username)
{
    public Gender? Gender { get; set; }
    public DateTime? CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public static UserDto FromUser(User user) => new(user.Id, user.Username)
    {
        Gender = user.Gender,
        
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };
}