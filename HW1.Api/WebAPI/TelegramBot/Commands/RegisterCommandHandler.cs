using HW1.Api.Domain.Contracts.Services;
using HW1.Api.Domain.Contracts.Telegram;
using HW1.Api.Domain.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace HW1.Api.WebAPI.TelegramBot.Commands;

public class RegisterCommandHandler : BaseCommandHandler
{
    private readonly Dictionary<long, UserRegistrationData> _registrationSessions = new();

    public override string Command => "/register";
    public override string Description => "Регистрация в системе";

    public RegisterCommandHandler(
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
                "Сначала выполните /start для регистрации в боте",
                cancellationToken);
            return;
        }

        var telegramUser = await _telegramUserService.GetUserAsync(message.From.Id);
        if (telegramUser?.SystemUserId != null)
        {
            await _botService.SendMessageAsync(
                message.Chat.Id,
                "Вы уже зарегистрированы в системе!\nИспользуйте /profile для просмотра вашего профиля",
                cancellationToken);
            return;
        }

        // Начинаем процесс регистрации
        _registrationSessions[message.From.Id] = new UserRegistrationData
        {
            TelegramUserId = message.From.Id,
            ChatId = message.Chat.Id,
            Step = RegistrationStep.Username
        };

        await AskForUsername(message.Chat.Id, cancellationToken);
    }

    public override async Task HandleCallbackAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        // Обработка callback'ов для выбора пола
        if (callbackQuery.Data?.StartsWith("gender:") == true && 
            _registrationSessions.TryGetValue(callbackQuery.From.Id, out var session))
        {
            var gender = callbackQuery.Data.Split(':')[1];
            session.Gender = gender;
            session.Step = RegistrationStep.Password;

            await _botService.SendMessageAsync(
                callbackQuery.Message.Chat.Id,
                "🔒 Отлично! Теперь придумайте надежный пароль (минимум 6 символов):",
                cancellationToken);

            await _botService.SendMessageAsync(
                callbackQuery.Message.Chat.Id,
                "💡 <b>Советы по паролю:</b>\n" +
                "• Используйте буквы, цифры и специальные символы\n" +
                "• Не используйте простые пароли\n" +
                "• Минимум 6 символов",
                cancellationToken);
        }
    }

    public async Task HandleRegistrationStepAsync(Message message, CancellationToken cancellationToken)
    {
        if (!_registrationSessions.TryGetValue(message.From.Id, out var session))
            return;

        try
        {
            switch (session.Step)
            {
                case RegistrationStep.Username:
                    await HandleUsernameStep(message, session, cancellationToken);
                    break;

                case RegistrationStep.Email:
                    await HandleEmailStep(message, session, cancellationToken);
                    break;

                case RegistrationStep.Gender:
                    await HandleGenderStep(message, session, cancellationToken);
                    break;

                case RegistrationStep.Password:
                    await HandlePasswordStep(message, session, cancellationToken);
                    break;

                case RegistrationStep.Complete:
                    await CompleteRegistration(message, session, cancellationToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        catch (Exception ex)
        {
            await _botService.SendMessageAsync(
                message.Chat.Id,
                "Произошла ошибка при регистрации. Попробуйте снова /register",
                cancellationToken);
            
            _registrationSessions.Remove(message.From.Id);
        }
    }
    public Task<bool> IsUserInRegistrationAsync(long telegramUserId)
    {
        return Task.FromResult(_registrationSessions.ContainsKey(telegramUserId));
    }
    private async Task HandleUsernameStep(Message message, UserRegistrationData session, CancellationToken cancellationToken)
    {
        var username = message.Text?.Trim();
        
        if (string.IsNullOrEmpty(username) || username.Length < 3)
        {
            await _botService.SendMessageAsync(
                message.Chat.Id,
                "Имя пользователя должно содержать минимум 3 символа. Попробуйте еще раз:",
                cancellationToken);
            return;
        }

        // не занято ли имя пользователя
        var existingUser = await _userService.GetUserByUsernameAsync(username);
        if (existingUser != null)
        {
            await _botService.SendMessageAsync(
                message.Chat.Id,
                "Это имя пользователя уже занято. Пожалуйста, выберите другое:",
                cancellationToken);
            return;
        }

        session.Username = username;
        session.Step = RegistrationStep.Email;

        await _botService.SendMessageAsync(
            message.Chat.Id,
            "Отлично! Теперь введите ваш email:",
            cancellationToken);
    }

    private async Task HandleEmailStep(Message message, UserRegistrationData session, CancellationToken cancellationToken)
    {
        var email = message.Text?.Trim();

        if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
        {
            await _botService.SendMessageAsync(
                message.Chat.Id,
                "Пожалуйста, введите корректный email адрес:",
                cancellationToken);
            return;
        }

        session.Email = email;
        session.Step = RegistrationStep.Gender;

        await AskForGender(message.Chat.Id, cancellationToken);
    }

    private async Task HandleGenderStep(Message message, UserRegistrationData session, CancellationToken cancellationToken)
    {
        await _botService.SendMessageAsync(
            message.Chat.Id,
            "Пожалуйста, выберите ваш пол используя кнопки выше:",
            cancellationToken);
    }

    private async Task HandlePasswordStep(Message message, UserRegistrationData session, CancellationToken cancellationToken)
    {
        var password = message.Text?.Trim();

        if (string.IsNullOrEmpty(password) || password.Length < 6)
        {
            await _botService.SendMessageAsync(
                message.Chat.Id,
                "Пароль должен содержать минимум 6 символов. Попробуйте еще раз:",
                cancellationToken);
            return;
        }

        session.Password = password;
        session.Step = RegistrationStep.Complete;

        await ShowRegistrationSummary(message.Chat.Id, session, cancellationToken);
    }

    private async Task CompleteRegistration(Message message, UserRegistrationData session, CancellationToken cancellationToken)
    {
        if (message.Text?.ToLower() == "да")
        {
            try
            {
                // Создаем пользователя в системе
                var userDto = await _userService.CreateUserAsync(
                    session.Username!,
                    session.Password!
                );

                var telegramUser = await _telegramUserService.GetUserAsync(session.TelegramUserId);
                if (telegramUser != null)
                {

                }

                _registrationSessions.Remove(session.TelegramUserId);

                await _botService.SendMessageAsync(
                    message.Chat.Id,
                    @$"
🎉 <b>Регистрация завершена успешно!</b>

✅ Вы успешно зарегистрированы в системе.

<b>Ваши данные:</b>
👤 Имя пользователя: <code>{session.Username}</code>
📧 Email: <code>{session.Email}</code>
👤 Пол: {GetGenderDisplayName(session.Gender)}
🆔 ID: <code>{userDto.Id}</code>

Теперь вы можете использовать все возможности системы!
Используйте /profile для просмотра вашего профиля.
                    ".Trim(),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                await _botService.SendMessageAsync(
                    message.Chat.Id,
                    "Ошибка при создании пользователя. Попробуйте снова /register",
                    cancellationToken);
                
                _registrationSessions.Remove(session.TelegramUserId);
            }
        }
        else if (message.Text?.ToLower() == "нет")
        {
            _registrationSessions.Remove(session.TelegramUserId);
            await _botService.SendMessageAsync(
                message.Chat.Id,
                "Регистрация отменена. Если хотите начать заново, используйте /register",
                cancellationToken);
        }
        else
        {
            await _botService.SendMessageAsync(
                message.Chat.Id,
                "❓ Пожалуйста, ответьте 'Да' или 'Нет':",
                cancellationToken);
        }
    }

    private async Task AskForUsername(long chatId, CancellationToken cancellationToken)
    {
        await _botService.SendMessageAsync(
            chatId,
            @"
👤 <b>Регистрация в системе</b>

Давайте создадим вашу учетную запись!

📝 <b>Шаг 1 из 4: Имя пользователя</b>

Введите имя пользователя (от 3 до 20 символов):
• Можно использовать буквы, цифры и символ _
• Должно быть уникальным

💡 <i>Пример: ivan_petrov, anna2024, user_123</i>
            ".Trim(),
            cancellationToken);
    }

    private async Task AskForGender(long chatId, CancellationToken cancellationToken)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Мужской", "gender:M"),
                InlineKeyboardButton.WithCallbackData("Женский", "gender:F")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Не указывать", "gender:U")
            }
        });

        await _botService.SendMessageAsync(
            chatId,
            @"
👤 <b>Шаг 3 из 4: Пол</b>

Выберите ваш пол (необязательно):
            ".Trim(),
            cancellationToken);

        // Отправляем сообщение с кнопками
        await _botService.SendMessageAsync(
            chatId,
            "Выберите пол:",
            cancellationToken);
    }

    private async Task ShowRegistrationSummary(long chatId, UserRegistrationData session, CancellationToken cancellationToken)
    {
        var summary = @$"
📋 <b>Проверьте ваши данные:</b>

👤 <b>Имя пользователя:</b> <code>{session.Username}</code>
📧 <b>Email:</b> <code>{session.Email}</code>
👤 <b>Пол:</b> {GetGenderDisplayName(session.Gender)}

<b>Всё верно?</b>
Ответьте 'Да' для завершения регистрации или 'Нет' для отмены.
        ".Trim();

        await _botService.SendMessageAsync(chatId, summary, cancellationToken);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static string GetGenderDisplayName(string? gender) => gender?.ToUpper() switch
    {
        "M" => "Мужской",
        "F" => "Женский",
        _ => "Не указан"
    };
}

public class UserRegistrationData
{
    public long TelegramUserId { get; set; }
    public long ChatId { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Gender { get; set; }
    public string? Password { get; set; }
    public RegistrationStep Step { get; set; }
}

public enum RegistrationStep
{
    Username,
    Email,
    Gender,
    Password,
    Complete
}