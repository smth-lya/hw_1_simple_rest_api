namespace HW1.Api.Application.DTOs;

public record LoginResult(
    bool IsSuccess, 
    Guid? UserId = null, 
    string? Username = null, 
    string? Error = null);