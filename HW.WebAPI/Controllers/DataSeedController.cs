using Microsoft.AspNetCore.Mvc;

namespace HW.WebAPI.Controllers;

[ApiController]
[Route("api/seed")]
public class DataSeedController : ControllerBase
{
    private readonly IDataSeedService _seedService;
    private readonly ILogger<DataSeedController> _logger;

    public DataSeedController(IDataSeedService seedService, ILogger<DataSeedController> logger)
    {
        _seedService = seedService;
        _logger = logger;
    }

    [HttpPost("users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SeedUsers([FromQuery] int count = 10)
    {
        try
        {
            if (count <= 0 || count > 1000)
            {
                return BadRequest(new { error = "Количество должно быть от 1 до 1000" });
            }

            if (!await _seedService.DatabaseIsEmptyAsync())
            {
                return BadRequest(new { 
                    error = "База данных не пуста. Необходимо использовать force=true для перезаписи" 
                });
            }

            var seededCount = await _seedService.SeedTestUsersAsync(count);
            
            return Ok(new { 
                message = $"Успешно заполненные {seededCount} тестовые пользователи",
                count = seededCount 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Пользователи теста заполнения ошибок");
            return StatusCode(500, new { error = "Seed operation failed" });
        }
    }

    [HttpPost("users/force")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedUsersForce([FromQuery] int count = 10)
    {
        try
        {
            if (count <= 0 || count > 1000)
            {
                return BadRequest(new { error = "Количество должно быть от 1 до 1000" });
            }

            await _seedService.ClearTestDataAsync();
            var seededCount = await _seedService.SeedTestUsersAsync(count);
            
            return Ok(new { 
                message = $"Успешно заполненные {seededCount} тестовые пользователи (forced)",
                count = seededCount 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Пользователи теста с принудительным заполнением ошибок");
            return StatusCode(500, new { error = "Не удалось выполнить операцию принудительной инициализации данных" });
        }
    }

    [HttpDelete("users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearUsers()
    {
        try
        {
            await _seedService.ClearTestDataAsync();
            return Ok(new { message = "Тестовые данные успешно очищены" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при очистке тестовых данных");
            return StatusCode(500, new { error = "Не удалось выполнить операцию очистки" });
        }
    }

    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSeedStatus()
    {
        try
        {
            var isEmpty = await _seedService.DatabaseIsEmptyAsync();
            return Ok(new { 
                isDatabaseEmpty = isEmpty,
                message = isEmpty ? "База данных пуста" : "База данных содержит данные"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении статуса начальных данны");
            return StatusCode(500, new { error = "Не удалось проверить статус" });
        }
    }
}