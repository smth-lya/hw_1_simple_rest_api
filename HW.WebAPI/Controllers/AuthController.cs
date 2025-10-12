using HW.Application.Services.Interfaces;
using HW.WebAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HW.WebAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request.Username, request.Password);

            if (!result.IsSuccess)
            {
                return Unauthorized(new { error = result.Error });
            }
            
            return Ok(new LoginResponse(result.UserId.ToString(), result.Username));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка входа в систему для пользователя {Username}", request.Username);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}