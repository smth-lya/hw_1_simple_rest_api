namespace HW.WebAPI.Models;

public record RegisterRequest(
    string Username, 
    string Password);
    
public record UsersFilterRequest(
    DateTime? FromDate,
    DateTime? ToDate);

public record UpdateRequest(
    string? Username, 
    string? Password);