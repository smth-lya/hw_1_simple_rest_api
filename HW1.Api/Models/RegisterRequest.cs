using System.ComponentModel.DataAnnotations;

namespace HW1.Api.Models;

public record RegisterRequest(
    string Username, 
    string Password);