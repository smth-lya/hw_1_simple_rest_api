using HW1.Api.Domain.Contracts.Telegram;

namespace HW1.Api.Infrastructure.Telegram;

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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Telegram Bot Background Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var botService = scope.ServiceProvider.GetRequiredService<ITelegramBotService>();
                
                await botService.StartAsync(stoppingToken);
                
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Telegram Bot Background Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Telegram Bot Background Service");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Telegram Bot Background Service is stopping");

        using var scope = _serviceProvider.CreateScope();
        var botService = scope.ServiceProvider.GetRequiredService<ITelegramBotService>();
        
        await botService.StopAsync(cancellationToken);
        
        await base.StopAsync(cancellationToken);
    }
}