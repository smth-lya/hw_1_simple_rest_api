using System.Text;
using HW1.Api.Domain.Contracts.Services;
using HW1.Api.Domain.Contracts.Telegram;
using HW1.Api.Domain.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using User = Telegram.Bot.Types.User;

namespace HW1.Api.WebAPI.TelegramBot.Commands;

public class ProfileCommandHandler : BaseCommandHandler
{
    public override string Command => "/profile";
    public override string Description => "Мой профиль";

    public ProfileCommandHandler(
        ITelegramBotService botService,
        IUserService userService,
        ITelegramUserService telegramUserService,
        ILogger<ProfileCommandHandler> logger)
        : base(botService, userService, telegramUserService, logger) { }

    public override async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        using var activity = BeginCommandScope(message);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Processing profile command for user {UserId}", message.From?.Id);

            if (!await ValidateUserAccessAsync(message.From.Id, cancellationToken))
            {
                _logger.LogWarning("User {UserId} access denied for profile command", message.From?.Id);
                await _botService.SendMessageAsync(
                    message.Chat.Id, 
                    "❌ Сначала выполните /start для регистрации в боте", 
                    cancellationToken: cancellationToken);
                return;
            }

            var telegramUser = await _telegramUserService.GetUserAsync(message.From.Id);
            if (telegramUser == null)
            {
                _logger.LogWarning("Telegram user {UserId} not found", message.From?.Id);
                await _botService.SendMessageAsync(
                    message.Chat.Id, 
                    "❌ Пользователь не найден. Выполните /start", 
                    cancellationToken: cancellationToken);
                return;
            }

            var profileMessage = await BuildProfileMessageAsync(telegramUser, message.From);
            var keyboard = CreateProfileKeyboard(telegramUser);

            await _botService.SendMessageAsync(message.Chat.Id, profileMessage, keyboard, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "Profile command completed successfully in {ElapsedMs}ms for user {UserId}", 
                stopwatch.ElapsedMilliseconds, message.From?.Id);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex, 
                "Error processing profile command after {ElapsedMs}ms for user {UserId}", 
                stopwatch.ElapsedMilliseconds, message.From?.Id);
                
            await _botService.SendMessageAsync(
                message.Chat.Id, 
                "❌ Ошибка при загрузке профиля", 
                cancellationToken: cancellationToken);
        }
    }

    private async Task<string> BuildProfileMessageAsync(TelegramUser telegramUser, User telegramFrom)
    {
        _logger.LogDebug("Building profile message for user {UserId}", telegramUser.TelegramUserId);
        
        var profile = new StringBuilder();

        profile.AppendLine("👤 <b>Ваш профиль</b>");
        profile.AppendLine();

        // Информация из Telegram
        profile.AppendLine("<b>Telegram данные:</b>");
        profile.AppendLine($"   ID: <code>{telegramUser.TelegramUserId}</code>");
        profile.AppendLine($"   Имя: {telegramFrom.FirstName} {telegramFrom.LastName}");
        
        if (!string.IsNullOrEmpty(telegramUser.Username))
            profile.AppendLine($"   Username: @{telegramUser.Username}");
        
        profile.AppendLine($"   Зарегистрирован: {telegramUser.RegisteredAt:dd.MM.yyyy HH:mm}");
        profile.AppendLine($"   Последняя активность: {telegramUser.LastActivity:dd.MM.yyyy HH:mm}");
        profile.AppendLine();

        // Информация из системы
        if (telegramUser.SystemUserId.HasValue)
        {
            var systemUser = await _userService.GetUserByIdAsync(telegramUser.SystemUserId.Value);
            if (systemUser != null)
            {
                _logger.LogDebug("Including system user data for user {UserId}", telegramUser.TelegramUserId);
                
                profile.AppendLine("<b>Данные системы:</b>");
                profile.AppendLine($"   System ID: <code>{systemUser.Id}</code>");
                profile.AppendLine($"   Username: <code>{systemUser.Username}</code>");
                profile.AppendLine($"   Регистрация: {systemUser.CreatedAt:dd.MM.yyyy}");
                profile.AppendLine($"   Обновлен: {systemUser.UpdatedAt:dd.MM.yyyy}");
                
                if (systemUser.Roles.Count != 0)
                    profile.AppendLine($"   🎯 Роли: {string.Join(", ", systemUser.Roles)}");
                    
                profile.AppendLine();
                profile.AppendLine("⚠️ <i>Вы можете отвязать аккаунт, если больше не хотите использовать систему</i>");
            }
        }
        else
        {
            _logger.LogDebug("User {UserId} has no system profile", telegramUser.TelegramUserId);
            
            profile.AppendLine("❌ <b>Системный профиль:</b> Не зарегистрирован");
            profile.AppendLine("💡 Используйте /register для создания учетной записи в системе");
        }

        // Статистика
        profile.AppendLine();
        profile.AppendLine("<b>Статистика:</b>");
        
        var totalUsers = await _userService.GetTotalUsersCountAsync();
        var activeTelegramUsers = await _telegramUserService.GetActiveUsersCountAsync();
        
        profile.AppendLine($"   Всего пользователей в системе: {totalUsers}");
        profile.AppendLine($"   Пользователей бота: {activeTelegramUsers}");

        return profile.ToString();
    }

    private InlineKeyboardMarkup CreateProfileKeyboard(TelegramUser telegramUser)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        if (!telegramUser.SystemUserId.HasValue)
        {
            // Пользователь без системного аккаунта
            buttons.Add([
                InlineKeyboardButton.WithCallbackData("🚀 Зарегистрироваться в системе", $"{Command} register_from_profile")
            ]);
        }
        else
        {
            // Пользователь с привязанным системным аккаунтом
            buttons.Add([
                InlineKeyboardButton.WithCallbackData("🔄 Обновить профиль", $"{Command} refresh_profile"),
                InlineKeyboardButton.WithCallbackData("✏️ Редактировать", $"{Command} edit_profile")
            ]);
            
            buttons.Add([
                InlineKeyboardButton.WithCallbackData("🔗 Отвязать аккаунт", $"{Command} unlink_account")
            ]);
        }

        // Общие кнопки для всех пользователей
        buttons.Add([
            InlineKeyboardButton.WithCallbackData("📊 Статистика", $"{Command} show_stats"),
            InlineKeyboardButton.WithCallbackData("👥 Пользователи", $"{Command} show_users")
        ]);

        return new InlineKeyboardMarkup(buttons);
    }

    public override async Task HandleCallbackAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        using var activity = BeginCallbackScope(callbackQuery);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var data = callbackQuery.Data?.Split()[1];
            
            _logger.LogInformation("Processing profile callback: {CallbackData}", data);

            switch (data)
            {
                case "refresh_profile":
                    await HandleRefreshProfile(callbackQuery, cancellationToken);
                    break;
                    
                case "register_from_profile":
                    await HandleRegisterFromProfile(callbackQuery, cancellationToken);
                    break;
                    
                case "edit_profile":
                    await HandleEditProfile(callbackQuery, cancellationToken);
                    break;
                    
                case "unlink_account":
                    await HandleUnlinkAccount(callbackQuery, cancellationToken);
                    break;
                    
                case "show_stats":
                    await HandleShowStats(callbackQuery, cancellationToken);
                    break;
                    
                case "show_users":
                    await HandleShowUsers(callbackQuery, cancellationToken);
                    break;
                    
                case "confirm_unlink":
                    await HandleConfirmUnlink(callbackQuery, cancellationToken);
                    break;
                    
                case "cancel_unlink":
                    await HandleCancelUnlink(callbackQuery, cancellationToken);
                    break;
                    
                default:
                    _logger.LogWarning("Unknown profile callback data: {CallbackData}", data);
                    break;
            }

            await _botService.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "Profile callback processed in {ElapsedMs}ms for user {UserId}", 
                stopwatch.ElapsedMilliseconds, callbackQuery.From.Id);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex, 
                "Error processing profile callback after {ElapsedMs}ms for user {UserId}", 
                stopwatch.ElapsedMilliseconds, callbackQuery.From.Id);
        }
    }

    private async Task HandleRefreshProfile(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Refreshing profile for user {UserId}", callbackQuery.From.Id);

        await _telegramUserService.UpdateUserActivityAsync(callbackQuery.From.Id);

        var telegramUser = await _telegramUserService.GetUserAsync(callbackQuery.From.Id);
        if (telegramUser != null)
        {
            var profileMessage = await BuildProfileMessageAsync(telegramUser, callbackQuery.From);
            var keyboard = CreateProfileKeyboard(telegramUser);

            await _botService.SendMessageAsync(
                callbackQuery.Message.Chat.Id, 
                "✅ Профиль обновлен!\n\n" + profileMessage, 
                keyboard, 
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleRegisterFromProfile(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initiating registration from profile for user {UserId}", callbackQuery.From.Id);

        await _botService.SendMessageAsync(
            callbackQuery.Message.Chat.Id, 
            "🚀 Начинаем регистрацию в системе...", 
            cancellationToken: cancellationToken);

        var message = new Message
        {
            From = callbackQuery.From,
            Chat = callbackQuery.Message.Chat,
            Text = "/register"
        };

        await HandleAsync(message, cancellationToken);
    }

    private async Task HandleEditProfile(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogDebug("User {UserId} requested profile edit", callbackQuery.From.Id);

        await _botService.SendMessageAsync(
            callbackQuery.Message.Chat.Id, 
            "✏️ <b>Редактирование профиля</b>\n\n" +
            "В настоящее время редактирование профиля доступно только через веб-интерфейс.\n\n" +
            "🌐 <a href=\"http://localhost:8080/swagger\">Перейти в веб-интерфейс</a>", 
            cancellationToken: cancellationToken);
    }

    private async Task HandleUnlinkAccount(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} initiated account unlinking", callbackQuery.From.Id);

        var confirmationMessage = """
            ⚠️ <b>Подтверждение отвязки аккаунта</b>
            
            Вы уверены, что хотите отвязать ваш Telegram аккаунт от системы?
            
            <b>Последствия:</b>
            • Вы потеряете доступ к системным функциям
            • Ваши данные останутся в системе, но будут недоступны через бота
            • Для повторного доступа потребуется новая регистрация
            
            Это действие можно отменить позже.
            """;

        var confirmationKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Да, отвязать", $"{Command} confirm_unlink"),
                InlineKeyboardButton.WithCallbackData("❌ Отмена", $"{Command} cancel_unlink")
            }
        });

        await _botService.SendMessageAsync(
            callbackQuery.Message.Chat.Id,
            confirmationMessage,
            confirmationKeyboard,
            cancellationToken: cancellationToken);
    }

    private async Task HandleConfirmUnlink(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} confirmed account unlinking", callbackQuery.From.Id);

        try
        {
            await _telegramUserService.UnlinkFromSystemUserAsync(callbackQuery.From.Id);
            
            _logger.LogInformation("Account successfully unlinked for user {UserId}", callbackQuery.From.Id);

            var successMessage = """
                ✅ <b>Аккаунт успешно отвязан!</b>
                
                Ваш Telegram аккаунт больше не связан с системой.
                
                Вы можете:
                • Продолжить использовать базовые функции бота
                • Зарегистрироваться заново командой /register
                • Обратиться к администратору при необходимости
                """;

            await _botService.SendMessageAsync(
                callbackQuery.Message.Chat.Id,
                successMessage,
                cancellationToken: cancellationToken);

            // Обновляем профиль чтобы показать новые кнопки
            var telegramUser = await _telegramUserService.GetUserAsync(callbackQuery.From.Id);
            if (telegramUser != null)
            {
                var profileMessage = await BuildProfileMessageAsync(telegramUser, callbackQuery.From);
                var keyboard = CreateProfileKeyboard(telegramUser);

                await _botService.SendMessageAsync(
                    callbackQuery.Message.Chat.Id,
                    profileMessage,
                    keyboard,
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking account for user {UserId}", callbackQuery.From.Id);
            
            await _botService.SendMessageAsync(
                callbackQuery.Message.Chat.Id,
                "❌ <b>Ошибка при отвязке аккаунта</b>\n\nПожалуйста, попробуйте позже или обратитесь к администратору.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleCancelUnlink(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} cancelled account unlinking", callbackQuery.From.Id);

        await _botService.SendMessageAsync(
            callbackQuery.Message.Chat.Id,
            "❌ <b>Отвязка аккаунта отменена</b>\n\nВаш аккаунт остается привязанным к системе.",
            cancellationToken: cancellationToken);

        // Показываем обновленный профиль
        var telegramUser = await _telegramUserService.GetUserAsync(callbackQuery.From.Id);
        if (telegramUser != null)
        {
            var profileMessage = await BuildProfileMessageAsync(telegramUser, callbackQuery.From);
            var keyboard = CreateProfileKeyboard(telegramUser);

            await _botService.SendMessageAsync(
                callbackQuery.Message.Chat.Id,
                profileMessage,
                keyboard,
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleShowStats(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogDebug("User {UserId} requested stats from profile", callbackQuery.From.Id);

        await _botService.SendMessageAsync(
            callbackQuery.Message.Chat.Id, 
            "📊 Загружаем статистику...", 
            cancellationToken: cancellationToken);

        var message = new Message
        {
            From = callbackQuery.From,
            Chat = callbackQuery.Message.Chat,
            Text = "/stats"
        };

        await HandleAsync(message, cancellationToken);
    }

    private async Task HandleShowUsers(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogDebug("User {UserId} requested users list from profile", callbackQuery.From.Id);

        await _botService.SendMessageAsync(
            callbackQuery.Message.Chat.Id, 
            "👥 Загружаем список пользователей...", 
            cancellationToken: cancellationToken);

        var message = new Message
        {
            From = callbackQuery.From,
            Chat = callbackQuery.Message.Chat,
            Text = "/users"
        };

        await HandleAsync(message, cancellationToken);
    }
}