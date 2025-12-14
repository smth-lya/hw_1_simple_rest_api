namespace HW1.Api.Infrastructure.Kafka;

public class KafkaTopics
{
    public string UserEvents { get; set; } = "telegram.user.events";
    public string CommandEvents { get; set; } = "telegram.command.events";
    public string RegistrationEvents { get; set; } = "telegram.registration.events";
    public string ErrorEvents { get; set; } = "telegram.error.events";
    public string AnalyticsAlerts { get; set; } = "telegram.analytics.alerts";
}