using HW1.Api.Domain.Contracts.Services;
using HW1.Api.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace HW1.Api.WebAPI.Controllers;

[ApiController]
[Route("api/users/analytics")]
public class UserAnalyticsController : ControllerBase
{
    private readonly IUserAnalyticsService _analyticsService;
    private readonly ILogger<UserAnalyticsController> _logger;
    
    public UserAnalyticsController(
        IUserAnalyticsService analyticsService,
        ILogger<UserAnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    [HttpGet("earliest-registration")]
    public async Task<IActionResult> GetEarliestRegistrationDate()
    {
        try
        {
            var date = await _analyticsService.GetEarliestRegistrationDateAsync();
            return Ok(new { earliestRegistrationDate = date });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении самой ранней даты регистрации");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("latest-registration")]
    public async Task<IActionResult> GetLatestRegistrationDate()
    {
        try
        {
            var date = await _analyticsService.GetLatestRegistrationDateAsync();
            return Ok(new { latestRegistrationDate = date });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении последней даты регистрации");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("sorted")]
    public async Task<IActionResult> GetUsersSortedByUsername([FromQuery] bool ascending = true)
    {
        try
        {
            var users = await _analyticsService.GetUsersSortedByUsernameAsync(ascending);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sorted users");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("by-gender/{gender}")]
    public async Task<IActionResult> GetUsersByGender(Gender gender)
    {
        try
        {
            var users = await _analyticsService.GetUsersByGenderAsync(gender);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении пользователей по полу {Gender}", gender);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("total-count")]
    public async Task<IActionResult> GetTotalUsersCount()
    {
        try
        {
            var count = await _analyticsService.GetTotalUsersCountAsync();
            return Ok(new { totalUsers = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при подсчете общего количества пользователей");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("gender-stats")]
    public async Task<IActionResult> GetUsersCountByGender()
    {
        try
        {
            var stats = await _analyticsService.GetUsersCountByGenderAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении гендерной статистики");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}