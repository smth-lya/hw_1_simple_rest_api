using HW1.Api.WebAPI.TelegramBot.Commands; // TODO: убрать зависимость от enum в слое Web

namespace HW1.Api.Domain.Contracts.Services.Kafka;

public record UserEvent(
    long UserId,
    string EventType, // "Login", "Logout", "ProfileView", "SettingsUpdate"
    Dictionary<string, object> Metadata,
    DateTime Timestamp);

public record CommandEvent(
    string Command,
    long UserId,
    long ChatId,
    TimeSpan Duration,
    bool Success,
    DateTime Timestamp);

public record RegistrationEvent(
    long UserId,
    RegistrationStep Step,
    TimeSpan StepDuration,
    bool IsCompleted,
    DateTime Timestamp);

public record ErrorEvent(
    string ErrorType,
    string Command,
    long? UserId,
    string Message,
    string StackTrace,
    DateTime Timestamp);