using HW1.Api.Application.DTOs;
using HW1.Api.Domain.Contracts.Services;
using HW1.Api.Domain.Models;
using HW1.Api.WebAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HW1.Api.WebAPI.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;
    
    public UsersController(
        IUserService userService,
        ILogger<UsersController> logger) 
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType( StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser(RegisterRequest request)
    {
        try
        {
            var result = await _userService.CreateUserAsync(request.Username, request.Password);
            
            return CreatedAtAction(nameof(GetUser), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка регистрации для {Username}", request.Username);
            return StatusCode(500, new { error = "Registration failed" });
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            return user != null ? Ok(user) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось получить пользователя {UserId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsersPaged([FromQuery]PaginationRequest request)
    {
        try
        {
            var result = await _userService.GetUsersPagedAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении пользователей");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
    
    [HttpGet("filter")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsersByDate([FromQuery] UsersFilterRequest filter)
    {
        try
        {
            var users = await _userService.GetUsersByDateRangeAsync(filter.FromDate, filter.ToDate);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering users");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
    
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(Guid id, UpdateRequest request)
    {
        try
        {
            await _userService.UpdateUserAsync(id, request.Username, request.Password);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось удалить пользователя {UserId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
    
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        try
        {
            await _userService.DeleteUserAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось удалить пользователя {UserId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}