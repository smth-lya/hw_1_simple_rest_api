using HW1.Api.Domain.Contracts.Services;
using HW1.Api.Domain.Contracts.Telegram;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace HW1.Api.WebAPI.TelegramBot.Commands;

public class StartCommandHandler : BaseCommandHandler
{
    public override string Command => "/start";
    public override string Description => "Запуск бота и регистрация";

    public StartCommandHandler(
        ITelegramBotService botService,
        IUserService userService,
        ITelegramUserService telegramUserService)
        : base(botService, userService, telegramUserService) { }

    public override async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        var telegramUser = await _telegramUserService.RegisterUserAsync(
            message.From.Id,
            message.Chat.Id,
            message.From.Username ?? string.Empty,
            message.From.FirstName,
            message.From.LastName ?? string.Empty
        );

        var welcomeMessage = @$"
👋 Добро пожаловать, {message.From.FirstName}!

Я - бот для управления пользователями системы.

📋 Доступные команды:
/start - Запуск бота
/help - Помощь и список команд
/profile - Мой профиль
/users - Список пользователей
/stats - Статистика системы
/register - Регистрация в системе

Для получения помощи по конкретной команде используйте /help [команда]
".Trim();

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("Статистика"), new KeyboardButton("Пользователи") },
            new[] { new KeyboardButton("Мой профиль"), new KeyboardButton("ℹПомощь") }
        })
        {
            ResizeKeyboard = true
        };

        await _botService.SendMessageAsync(
            message.Chat.Id,
            welcomeMessage,
            cancellationToken);
    }
}