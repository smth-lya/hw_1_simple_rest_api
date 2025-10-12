namespace HW.Application.DTOs.Commands;

public record UpdateUserCommand(Guid UserId, string? Username, string? Password);
