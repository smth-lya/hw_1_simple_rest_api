namespace HW1.Api.WebAPI.Models;

public record RegisterRequest(
    string Username, 
    string Password);
    
public record UpdateRequest(
    string? Username, 
    string? Password);
    
public record UserResponse(
    Guid Id,
    string Username);