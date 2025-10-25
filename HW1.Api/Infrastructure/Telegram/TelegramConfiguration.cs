namespace HW1.Api.Infrastructure.Telegram;

public class TelegramBotConfiguration
{
    public string BotToken { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public long[] AdminUserIds { get; set; } = [];
    public int MaxMessageLength { get; set; } = 4096;
    public bool UseWebhook { get; set; } = false;
}