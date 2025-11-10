using HW1.Api.Domain.Contracts.Telegram;

namespace HW1.Api.Infrastructure.Telegram;

// Фоновый сервис для бота
public class TelegramBotBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TelegramBotBackgroundService> _logger;

    public TelegramBotBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<TelegramBotBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    // Фоновый сервис при запуске производит запуск бота
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Service"] = "TelegramBotBackgroundService",
            ["Operation"] = "ExecuteAsync"
        });
        
        _logger.LogInformation("Telegram Bot Background Service is starting");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var botService = scope.ServiceProvider.GetRequiredService<ITelegramBotService>();
                
            await botService.RunAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Telegram Bot Background Service is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Telegram Bot Background Service");
            throw;
        }
    }
}