namespace HW1.Api.Infrastructure.Kafka;

public class KafkaBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KafkaBackgroundService> _logger;
    private readonly KafkaSettings _settings;
    private readonly TimeSpan _pollTimeout = TimeSpan.FromSeconds(1);

    public KafkaBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<KafkaSettings> settings,
        ILogger<KafkaBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kafka Background Service is starting");
        
        // Ждем запуска основного приложения
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var consumerService = scope.ServiceProvider.GetRequiredService<KafkaConsumerService>();
                
                await consumerService.StartConsumingAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Kafka Background Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Kafka Background Service. Restarting in 10 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}