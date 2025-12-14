using System.Text.Json;
using Confluent.Kafka;
using HW1.Api.Domain.Contracts.Services.Kafka;
using HW1.Api.Domain.Contracts.Telegram;
using Microsoft.Extensions.Options;

namespace HW1.Api.Infrastructure.Kafka;

public class KafkaConsumerService : IDisposable
{
    private readonly IConsumer<Ignore, string> _consumer;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly KafkaSettings _settings;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<string> _topics;

    public KafkaConsumerService(
        IConsumer<Ignore, string> consumer,
        IOptions<KafkaSettings> settings,
        ILogger<KafkaConsumerService> logger,
        IServiceProvider serviceProvider)
    {
        _consumer = consumer;
        _logger = logger;
        _settings = settings.Value;
        _serviceProvider = serviceProvider;
        
        _topics = new List<string>
        {
            _settings.Topics.CommandEvents,
            _settings.Topics.RegistrationEvents,
            _settings.Topics.ErrorEvents,
            _settings.Topics.AnalyticsAlerts
        };
    }

    public async Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        _consumer.Subscribe(_topics);
        _logger.LogInformation("Subscribed to topics: {Topics}", string.Join(", ", _topics));

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(cancellationToken);
                
                if (consumeResult?.Message?.Value == null)
                    continue;

                await ProcessMessageAsync(consumeResult.Topic, consumeResult.Message.Value, cancellationToken);
                
                // Ручное подтверждение обработки
                _consumer.Commit(consumeResult);
                _consumer.StoreOffset(consumeResult);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Error consuming message from Kafka: {Error}", ex.Error.Reason);
                
                if (ex.Error.IsFatal)
                {
                    throw;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Consumption was cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error consuming from Kafka");
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    private async Task ProcessMessageAsync(string topic, string messageJson, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        try
        {
            switch (topic)
            {
                case var t when t == _settings.Topics.CommandEvents:
                    await ProcessCommandEventAsync(messageJson, scope, cancellationToken);
                    break;

                case var t when t == _settings.Topics.RegistrationEvents:
                    await ProcessRegistrationEventAsync(messageJson, scope, cancellationToken);
                    break;

                case var t when t == _settings.Topics.ErrorEvents:
                    await ProcessErrorEventAsync(messageJson, scope, cancellationToken);
                    break;

                case var t when t == _settings.Topics.AnalyticsAlerts:
                    await ProcessAnalyticsAlertAsync(messageJson, scope, cancellationToken);
                    break;

                default:
                    _logger.LogWarning("Unknown topic: {Topic}", topic);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from topic {Topic}", topic);
        }
    }

    private async Task ProcessCommandEventAsync(string json, IServiceScope scope, CancellationToken cancellationToken)
    {
        var commandEvent = JsonSerializer.Deserialize<CommandEvent>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (commandEvent == null) return;

        _logger.LogInformation("Processing command event: {Command} from user {UserId}", 
            commandEvent.Command, commandEvent.UserId);

        // Здесь можно:
        // 1. Сохранять в OpenSearch для аналитики
        // 2. Отправлять уведомления
        // 3. Обновлять кэш
    }

    private async Task ProcessRegistrationEventAsync(string json, IServiceScope scope, CancellationToken cancellationToken)
    {
        var registrationEvent = JsonSerializer.Deserialize<RegistrationEvent>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (registrationEvent == null) return;

        _logger.LogInformation("Processing registration event for user {UserId} at step {Step}", 
            registrationEvent.UserId, registrationEvent.Step);

        // Отправляем приветственное письмо при успешной регистрации
        if (registrationEvent.IsCompleted)
        {
            try
            {
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                await emailService.SendWelcomeEmailAsync(registrationEvent.UserId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to user {UserId}", registrationEvent.UserId);
            }
        }
    }

    private async Task ProcessErrorEventAsync(string json, IServiceScope scope, CancellationToken cancellationToken)
    {
        var errorEvent = JsonSerializer.Deserialize<ErrorEvent>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (errorEvent == null) return;

        _logger.LogWarning("Processing error event: {ErrorType} for command {Command}", 
            errorEvent.ErrorType, errorEvent.Command);

        // Отправляем алерт в Telegram админу
        if (errorEvent.ErrorType == "Critical")
        {
            try
            {
                var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramBotService>();
                await telegramService.SendMessageAsync(
                    chatId: 123456789, // ID админа
                    message: $"🚨 Critical Error: {errorEvent.ErrorType}\nCommand: {errorEvent.Command}\nMessage: {errorEvent.Message}",
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send critical error alert");
            }
        }
    }

    private async Task ProcessAnalyticsAlertAsync(string json, IServiceScope scope, CancellationToken cancellationToken)
    {
        var alert = JsonDocument.Parse(json).RootElement;
        
        if (!alert.TryGetProperty("type", out var typeProperty))
            return;

        var alertType = typeProperty.GetString();
        
        _logger.LogInformation("Processing analytics alert: {AlertType}", alertType);

        // Обработка различных типов алертов
        switch (alertType)
        {
            case "SlowCommand":
                _logger.LogWarning("Slow command detected: {@Alert}", alert);
                break;
                
            case "AbandonedRegistration":
                _logger.LogWarning("Abandoned registration: {@Alert}", alert);
                break;
                
            case "HighErrorRate":
                _logger.LogError("High error rate detected: {@Alert}", alert);
                break;
        }
    }

    public void Dispose()
    {
        _consumer?.Close();
        _consumer?.Dispose();
    }
}