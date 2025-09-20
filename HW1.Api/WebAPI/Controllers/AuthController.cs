using HW1.Api.Domain.Contracts.Repositories;
using HW1.Api.Domain.Contracts.Security;
using HW1.Api.WebAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HW1.Api.WebAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        try
        {
            var user = await _userRepository.GetUserByUsernameAsync(request.Username);
            if (user == null)
                return Unauthorized(new { error = "Invalid credentials" });

            if (!_passwordHasher.VerifyHashedPassword(request.Password, user.PasswordHash))
                return Unauthorized(new { error = "Invalid credentials" });
            
            return Ok(new LoginResponse(
                user.Id.ToString(),
                user.Username));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка входа в систему для пользователя {Username}", request.Username);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}