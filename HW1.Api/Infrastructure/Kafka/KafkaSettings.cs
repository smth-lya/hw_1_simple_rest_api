namespace HW1.Api.Infrastructure.Kafka;

public class KafkaSettings
{
    /// <summary>
    /// Список брокеров Kafka
    /// </summary>
    public string BootstrapServers { get; set; } = "kafka:19092";

    /// <summary>
    /// Группа потребителей
    /// </summary>
    public string ConsumerGroup { get; set; } = $"telegram-bot-{Environment.MachineName}";

    /// <summary>
    /// Топики Kafka
    /// </summary>
    public KafkaTopics Topics { get; set; } = new KafkaTopics();
}