using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using HW1.Api.Domain.Contracts.Services.Kafka;
using HW1.Api.WebAPI.TelegramBot.Commands;
using Microsoft.Extensions.Options;

namespace HW1.Api.Infrastructure.Kafka;

public class KafkaProducerService : IAnalyticsService, IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly KafkaSettings _settings;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly CircuitBreaker _circuitBreaker;

    public KafkaProducerService(
        IProducer<Null, string> producer,
        IOptions<KafkaSettings> settings,
        ILogger<KafkaProducerService> logger)
    {
        _producer = producer;
        _logger = logger;
        _settings = settings.Value;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        _circuitBreaker = new CircuitBreaker(
            failureThreshold: 3,
            successThreshold: 2,
            timeout: TimeSpan.FromSeconds(30));
    }

    public async Task PublishUserEventAsync(UserEvent userEvent, CancellationToken cancellationToken = default)
    {
        await PublishEventAsync(_settings.Topics.UserEvents, userEvent, cancellationToken);
    }

    public async Task PublishCommandEventAsync(CommandEvent commandEvent, CancellationToken cancellationToken = default)
    {
        await PublishEventAsync(_settings.Topics.CommandEvents, commandEvent, cancellationToken);
        
        // Дополнительная аналитика для медленных команд
        if (commandEvent.Duration > TimeSpan.FromSeconds(3))
        {
            var slowCommandAlert = new
            {
                Type = "SlowCommand",
                Command = commandEvent.Command,
                UserId = commandEvent.UserId,
                DurationMs = commandEvent.Duration.TotalMilliseconds,
                ChatId = commandEvent.ChatId,
                Timestamp = DateTime.UtcNow
            };
            
            await PublishEventAsync(_settings.Topics.AnalyticsAlerts, slowCommandAlert, cancellationToken);
        }
    }

    public async Task PublishRegistrationEventAsync(RegistrationEvent registrationEvent, CancellationToken cancellationToken = default)
    {
        await PublishEventAsync(_settings.Topics.RegistrationEvents, registrationEvent, cancellationToken);
        
        // Аналитика брошенных регистраций
        if (registrationEvent.Step == RegistrationStep.Password && !registrationEvent.IsCompleted)
        {
            var abandonedAlert = new
            {
                Type = "AbandonedRegistration",
                UserId = registrationEvent.UserId,
                Step = registrationEvent.Step.ToString(),
                DurationMs = registrationEvent.StepDuration.TotalMilliseconds,
                Timestamp = DateTime.UtcNow
            };
            
            await PublishEventAsync(_settings.Topics.AnalyticsAlerts, abandonedAlert, cancellationToken);
        }
    }

    public async Task PublishErrorEventAsync(ErrorEvent errorEvent, CancellationToken cancellationToken = default)
    {
        await PublishEventAsync(_settings.Topics.ErrorEvents, errorEvent, cancellationToken);
    }

    private async Task PublishEventAsync<T>(string topic, T eventData, CancellationToken cancellationToken)
    {
        try
        {
            if (!_circuitBreaker.AllowRequest())
            {
                _logger.LogWarning("Circuit breaker is open. Skipping event publish to {Topic}", topic);
                await SaveToFallbackStorage(eventData, topic);
                return;
            }

            var json = JsonSerializer.Serialize(eventData, _jsonOptions);
            var message = new Message<Null, string>
            {
                Value = json,
                Headers = new Headers
                {
                    { "event-type", Encoding.UTF8.GetBytes(typeof(T).Name) },
                    { "timestamp", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("o")) },
                    { "service", Encoding.UTF8.GetBytes("telegram-bot") }
                }
            };

            var deliveryResult = await _producer.ProduceAsync(topic, message, cancellationToken);
            
            _circuitBreaker.RecordSuccess();
            
            _logger.LogTrace("Event published to {Topic} [{Partition}] at offset {Offset}",
                topic, deliveryResult.Partition.Value, deliveryResult.Offset.Value);
        }
        catch (ProduceException<Null, string> ex)
        {
            _circuitBreaker.RecordFailure();
            
            _logger.LogError(ex, "Failed to publish event to {Topic}. Error: {Error}", 
                topic, ex.Error.Reason);
            
            await SaveToFallbackStorage(eventData, topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error publishing event to {Topic}", topic);
            await SaveToFallbackStorage(eventData, topic);
        }
    }

    private async Task SaveToFallbackStorage<T>(T eventData, string topic)
    {
        try
        {
            // Сохраняем в локальный файл или БД для последующей отправки
            var fallbackPath = Path.Combine("kafka_fallback", $"{topic}_{DateTime.UtcNow:yyyyMMdd}");
            Directory.CreateDirectory(Path.GetDirectoryName(fallbackPath)!);
            
            var json = JsonSerializer.Serialize(eventData, _jsonOptions);
            var fileName = $"{Guid.NewGuid()}.json";
            var filePath = Path.Combine(fallbackPath, fileName);
            
            await File.WriteAllTextAsync(filePath, json);
            
            _logger.LogInformation("Event saved to fallback storage: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save event to fallback storage");
        }
    }

    public async Task FlushAsync(TimeSpan timeout)
    {
        _producer.Flush(timeout);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}

// Circuit Breaker для отказоустойчивости
public class CircuitBreaker
{
    private readonly int _failureThreshold;
    private readonly int _successThreshold;
    private readonly TimeSpan _timeout;
    
    private CircuitState _state = CircuitState.Closed;
    private int _failureCount = 0;
    private int _successCount = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;

    public CircuitBreaker(int failureThreshold, int successThreshold, TimeSpan timeout)
    {
        _failureThreshold = failureThreshold;
        _successThreshold = successThreshold;
        _timeout = timeout;
    }

    public bool AllowRequest()
    {
        if (_state == CircuitState.Open)
        {
            if (DateTime.UtcNow - _lastFailureTime > _timeout)
            {
                _state = CircuitState.HalfOpen;
                return true;
            }
            return false;
        }
        return true;
    }

    public void RecordSuccess()
    {
        if (_state == CircuitState.HalfOpen)
        {
            _successCount++;
            if (_successCount >= _successThreshold)
            {
                Reset();
            }
        }
        else
        {
            Reset();
        }
    }

    public void RecordFailure()
    {
        if (_state == CircuitState.HalfOpen)
        {
            _state = CircuitState.Open;
            _lastFailureTime = DateTime.UtcNow;
        }
        else
        {
            _failureCount++;
            if (_failureCount >= _failureThreshold)
            {
                _state = CircuitState.Open;
                _lastFailureTime = DateTime.UtcNow;
            }
        }
    }

    private void Reset()
    {
        _state = CircuitState.Closed;
        _failureCount = 0;
        _successCount = 0;
        _lastFailureTime = DateTime.MinValue;
    }

    private enum CircuitState
    {
        Closed,
        Open,
        HalfOpen
    }
}