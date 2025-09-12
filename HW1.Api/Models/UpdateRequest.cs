using System.ComponentModel.DataAnnotations;

namespace HW1.Api.Models;

public record UpdateRequest(
    string? Username, 
    string? Password);