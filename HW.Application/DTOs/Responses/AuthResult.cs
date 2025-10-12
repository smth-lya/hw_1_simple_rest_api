namespace HW.Application.DTOs.Responses;

public record AuthResult(
    bool IsSuccess, 
    Guid? UserId = null, 
    string? Username = null, 
    string? Error = null);