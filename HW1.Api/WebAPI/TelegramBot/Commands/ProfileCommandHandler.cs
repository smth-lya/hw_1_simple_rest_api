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
        ITelegramUserService telegramUserService)
        : base(botService, userService, telegramUserService) { }

    public override async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        if (!await ValidateUserAccessAsync(message.From.Id, cancellationToken))
        {
            await _botService.SendMessageAsync(
                message.Chat.Id,
                "❌ Сначала выполните /start для регистрации в боте",
                cancellationToken);
            return;
        }

        var telegramUser = await _telegramUserService.GetUserAsync(message.From.Id);
        if (telegramUser == null)
        {
            await _botService.SendMessageAsync(
                message.Chat.Id,
                "❌ Пользователь не найден. Выполните /start",
                cancellationToken);
            return;
        }

        try
        {
            var profileMessage = await BuildProfileMessageAsync(telegramUser, message.From);
            var keyboard = CreateProfileKeyboard(telegramUser);

            await _botService.SendMessageAsync(
                message.Chat.Id,
                profileMessage,
                cancellationToken);
        }
        catch (Exception ex)
        {
            await _botService.SendMessageAsync(
                message.Chat.Id,
                "❌ Ошибка при загрузке профиля",
                cancellationToken);
        }
    }

    private async Task<string> BuildProfileMessageAsync(TelegramUser telegramUser, User telegramFrom)
    {
        var profile = new StringBuilder();

        profile.AppendLine("👤 <b>Ваш профиль</b>");
        profile.AppendLine();

        // Информация из Telegram
        profile.AppendLine("📱 <b>Telegram данные:</b>");
        profile.AppendLine($"   🆔 ID: <code>{telegramUser.TelegramUserId}</code>");
        profile.AppendLine($"   👤 Имя: {telegramFrom.FirstName} {telegramFrom.LastName}".Trim());
        
        if (!string.IsNullOrEmpty(telegramUser.Username))
            profile.AppendLine($"   📝 Username: @{telegramUser.Username}");
        
        profile.AppendLine($"   📅 Зарегистрирован: {telegramUser.RegisteredAt:dd.MM.yyyy HH:mm}");
        profile.AppendLine($"   ⏰ Последняя активность: {telegramUser.LastActivity:dd.MM.yyyy HH:mm}");
        profile.AppendLine();

        // Информация из системы
        if (telegramUser.SystemUserId.HasValue)
        {
            var systemUser = await _userService.GetUserByIdAsync(telegramUser.SystemUserId.Value);
            if (systemUser != null)
            {
                profile.AppendLine("🌐 <b>Данные системы:</b>");
                profile.AppendLine($"   🆔 System ID: <code>{systemUser.Id}</code>");
                profile.AppendLine($"   👤 Username: <code>{systemUser.Username}</code>");
                profile.AppendLine($"   📅 Регистрация: {systemUser.CreatedAt:dd.MM.yyyy}");
                profile.AppendLine($"   🔄 Обновлен: {systemUser.UpdatedAt:dd.MM.yyyy}");
                
                // if (systemUser.Roles.Any())
                //     profile.AppendLine($"   🎯 Роли: {string.Join(", ", systemUser.Roles)}");
            }
        }
        else
        {
            profile.AppendLine("❌ <b>Системный профиль:</b> Не зарегистрирован");
            profile.AppendLine("💡 Используйте /register для создания учетной записи в системе");
        }

        // Статистика
        profile.AppendLine();
        profile.AppendLine("📊 <b>Статистика:</b>");
        
        var totalUsers = await _userService.GetTotalUsersCountAsync();
        var activeTelegramUsers = await _telegramUserService.GetActiveUsersCountAsync();
        
        profile.AppendLine($"   👥 Всего пользователей в системе: {totalUsers}");
        profile.AppendLine($"   🤖 Пользователей бота: {activeTelegramUsers}");

        return profile.ToString();
    }

    private static InlineKeyboardMarkup CreateProfileKeyboard(TelegramUser telegramUser)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        if (!telegramUser.SystemUserId.HasValue)
        {
            buttons.Add([
                InlineKeyboardButton.WithCallbackData("🚀 Зарегистрироваться в системе", "register_from_profile")
            ]);
        }
        else
        {
            buttons.Add([
                InlineKeyboardButton.WithCallbackData("🔄 Обновить профиль", "refresh_profile"),
                InlineKeyboardButton.WithCallbackData("✏️ Редактировать", "edit_profile")
            ]);
        }

        buttons.Add([
            InlineKeyboardButton.WithCallbackData("📊 Статистика", "show_stats"),
            InlineKeyboardButton.WithCallbackData("👥 Пользователи", "show_users")
        ]);

        return new InlineKeyboardMarkup(buttons);
    }

    public override async Task HandleCallbackAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        switch (callbackQuery.Data)
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
                
            case "show_stats":
                await HandleShowStats(callbackQuery, cancellationToken);
                break;
                
            case "show_users":
                await HandleShowUsers(callbackQuery, cancellationToken);
                break;
        }
    }

    private async Task HandleRefreshProfile(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        // Обновляем время активности
        await _telegramUserService.UpdateUserActivityAsync(callbackQuery.From.Id);

        // Перестраиваем сообщение профиля
        var telegramUser = await _telegramUserService.GetUserAsync(callbackQuery.From.Id);
        if (telegramUser != null)
        {
            var profileMessage = await BuildProfileMessageAsync(telegramUser, callbackQuery.From);
            var keyboard = CreateProfileKeyboard(telegramUser);

            await _botService.SendMessageAsync(
                callbackQuery.Message.Chat.Id,
                "✅ Профиль обновлен!\n\n" + profileMessage,
                cancellationToken);
        }
    }

    private async Task HandleRegisterFromProfile(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        await _botService.SendMessageAsync(
            callbackQuery.Message.Chat.Id,
            "🚀 Начинаем регистрацию в системе...",
            cancellationToken);

        // Имитируем отправку команды register
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
        await _botService.SendMessageAsync(
            callbackQuery.Message.Chat.Id,
            "✏️ <b>Редактирование профиля</b>\n\n" +
            "В настоящее время редактирование профиля доступно только через веб-интерфейс.\n\n" +
            "🌐 <a href=\"http://localhost:8080/swagger\">Перейти в веб-интерфейс</a>",
            cancellationToken);
    }

    private async Task HandleShowStats(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        await _botService.SendMessageAsync(
            callbackQuery.Message.Chat.Id,
            "📊 Загружаем статистику...",
            cancellationToken);

        // Имитируем отправку команды stats
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
        await _botService.SendMessageAsync(
            callbackQuery.Message.Chat.Id,
            "👥 Загружаем список пользователей...",
            cancellationToken);

        // Имитируем отправку команды users
        var message = new Message
        {
            From = callbackQuery.From,
            Chat = callbackQuery.Message.Chat,
            Text = "/users"
        };

        await HandleAsync(message, cancellationToken);
    }

    private static string GetGenderDisplay(Gender gender) => gender switch
    {
        Gender.Male => "Мужской",
        Gender.Female => "Женский",
        _ => "Не указан"
    };
}