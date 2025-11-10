using HW1.Api.Domain.Contracts.Services;
using HW1.Api.Domain.Contracts.Telegram;
using Microsoft.Extensions.Logging;
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
        ITelegramUserService telegramUserService,
        ILogger<StartCommandHandler> logger)
        : base(botService, userService, telegramUserService, logger)
    {
    }

    public override async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        using var activity = BeginCommandScope(message);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Processing start command from user {UserId}", message.From?.Id);

            await _telegramUserService.RegisterUserAsync(
                message.From.Id,
                message.Chat.Id,
                message.From.Username ?? string.Empty,
                message.From.FirstName,
                message.From.LastName ?? string.Empty
            );

            var welcomeMessage = $"""

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

                                  """.Trim();

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📊 Статистика", "/stats"),
                    InlineKeyboardButton.WithCallbackData("👥 Пользователи", "/users")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("👤 Мой профиль", "/profile"),
                    InlineKeyboardButton.WithCallbackData("ℹ Помощь", "/help")
                }
            });

            await _botService.SendMessageAsync(message.Chat.Id, welcomeMessage, keyboard, cancellationToken: cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "Start command completed successfully in {ElapsedMs}ms for user {UserId}", 
                stopwatch.ElapsedMilliseconds, message.From?.Id);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex, 
                "Error processing start command after {ElapsedMs}ms for user {UserId}", 
                stopwatch.ElapsedMilliseconds, message.From?.Id);
            throw;
        }
    }
    
    public override async Task HandleCallbackAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        using var activity = BeginCallbackScope(callbackQuery);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Processing start callback from user {UserId}: {CallbackData}", 
                callbackQuery.From.Id, callbackQuery.Data);

            if (callbackQuery.Data == null)
            {
                _logger.LogWarning("Empty callback data from user {UserId}", callbackQuery.From.Id);
                return;
            }

            var chatId = callbackQuery.Message?.Chat.Id ?? callbackQuery.From.Id;
            await _botService.SendMessageAsync(chatId, callbackQuery.Data, cancellationToken: cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "Start callback processed in {ElapsedMs}ms for user {UserId}", 
                stopwatch.ElapsedMilliseconds, callbackQuery.From.Id);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex, 
                "Error processing start callback after {ElapsedMs}ms for user {UserId}", 
                stopwatch.ElapsedMilliseconds, callbackQuery.From.Id);
        }
    }
}