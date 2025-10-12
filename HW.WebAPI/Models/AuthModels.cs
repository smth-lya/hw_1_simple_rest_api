namespace HW.WebAPI.Models;

public record LoginRequest(string Username, string Password);

public record LoginResponse(
    string? UserId = null,
    string? Username = null);