using System.ComponentModel.DataAnnotations;

namespace HW1.Api.WebAPI.Models;

public record RegisterRequest(
    string Username, 
    string Password);
    
public record UsersFilterRequest(
    DateTime? FromDate,
    DateTime? ToDate);

public record UpdateRequest(
    string? Username, 
    string? Password);