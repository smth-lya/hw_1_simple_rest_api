using HW1.Api.Domain.Contracts.Repositories;
using HW1.Api.Domain.Contracts.Security;
using HW1.Api.Domain.Models;
using HW1.Api.WebAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HW1.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<UsersController> _logger;
    
    public UsersController(
        IUserRepository userRepository, 
        IPasswordHasher passwordHasher,
        ILogger<UsersController> logger) 
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(RegisterRequest request)
    {
        try
        {
            var existingUser = await _userRepository.GetUserByUsernameAsync(request.Username);
            if (existingUser != null)
                return Conflict(new { error = "Имя пользователя уже существует"});

            var user = new User()
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                PasswordHash = _passwordHasher.HashPassword(request.Password)
            };

            await _userRepository.AddUserAsync(user);
            
            return CreatedAtAction(
                nameof(GetUser),
                new { id = user.Id },
                new UserResponse(user.Id, user.Username));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка регистрации для {Username}", request.Username);
            return StatusCode(500, new { error = "Registration failed" });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        try
        {
            var currentUser = await _userRepository.GetUserByIdAsync(id);
            if (currentUser == null)
                return NotFound();
            
            return Ok(new UserResponse(currentUser.Id, currentUser.Username));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось получить пользователя {UserId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, UpdateRequest request)
    {
        try
        {
            var currentUser = await _userRepository.GetUserByIdAsync(id);
            if (currentUser == null)
                return NotFound();

            if (!string.IsNullOrEmpty(request.Username))
            {
                var otherUser = await _userRepository.GetUserByUsernameAsync(request.Username);
                if (otherUser != null && otherUser.Id != id)
                    return BadRequest($"Username: {request.Username} уже существует");
            }

            var updatedUser = new User()
            {
                Id = id,
                Username = request.Username ?? currentUser.Username,
                PasswordHash = request.Password == null ? currentUser.PasswordHash : _passwordHasher.HashPassword(request.Password),
            };
    
            await _userRepository.UpdateUserAsync(updatedUser);
    
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось удалить пользователя {UserId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        try
        {
            await _userRepository.DeleteUserAsync(id);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось удалить пользователя {UserId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}