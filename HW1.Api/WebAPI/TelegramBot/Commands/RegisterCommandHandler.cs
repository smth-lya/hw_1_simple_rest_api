using HW1.Api.Domain.Contracts.Services;
using HW1.Api.Domain.Contracts.Telegram;
using HW1.Api.Domain.Models;
using HW1.Api.Infrastructure.Telegram;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace HW1.Api.WebAPI.TelegramBot.Commands;

public class RegisterCommandHandler : BaseCommandHandler
{
    private readonly IRegistrationStorage _registrationStorage;
    
    public override string Command => "/register";
    public override string Description => "Регистрация в системе";

    public RegisterCommandHandler(
        ITelegramBotService botService,
        IUserService userService,
        ITelegramUserService telegramUserService,
        IRegistrationStorage registrationStorage)
        : base(botService, userService, telegramUserService)
    {
        _registrationStorage = registrationStorage;
    }

    public override async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        if (!await ValidateUserAccessAsync(message.From.Id, cancellationToken))
        {
            await _botService.SendMessageAsync(
                message.Chat.Id,
                "Сначала выполните /start для регистрации в боте",
                cancellationToken: cancellationToken);
            return;
        }

        var telegramUser = await _telegramUserService.GetUserAsync(message.From.Id);
        if (telegramUser?.SystemUserId != null)
        {
            await _botService.SendMessageAsync(message.Chat.Id,
                "Вы уже зарегистрированы в системе!\nИспользуйте /profile для просмотра вашего профиля",
                cancellationToken: cancellationToken);
            return;
        }

        if (await _registrationStorage.IsUserInRegistrationAsync(message.From.Id))
        {
            await _botService.SendMessageAsync(
                message.Chat.Id,
                "Вы уже в процессе регистрации! Продолжайте вводить данные.", 
                cancellationToken: cancellationToken);
            return;
        }
        
        // Начинаем процесс регистрации
        var session = new UserRegistrationData
        {
            TelegramUserId = message.From.Id,
            ChatId = message.Chat.Id,
            Step = RegistrationStep.Username
        };

        await _registrationStorage.SetRegistrationStateAsync(message.From.Id, session);
        await AskForUsername(message.Chat.Id, cancellationToken);
    }

    public override async Task HandleCallbackAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        // Обработка callback'ов для выбора пола
        if (callbackQuery.Data?.Split()[1].StartsWith("gender:") == true)
        {
            var session = await _registrationStorage.GetRegistrationStateAsync(callbackQuery.From.Id);
            
            if (session == null)
                return;
            
            var gender = callbackQuery.Data.Split()[1].Split(':')[1];
            session.Gender = gender;
            session.Step = RegistrationStep.Password;
            
            await _registrationStorage.SetRegistrationStateAsync(callbackQuery.From.Id, session);

            await _botService.SendMessageAsync(
                callbackQuery.Message.Chat.Id, 
                """
                <b>Шаг 3 из 3: Пароль</b>
                
                Отлично! Теперь придумайте надежный пароль (минимум 6 символов):
                """, 
                cancellationToken: cancellationToken);

            await _botService.SendMessageAsync(callbackQuery.Message.Chat.Id,
                "💡 <b>Советы по паролю:</b>\n" +
                "• Используйте буквы, цифры и специальные символы\n" +
                "• Не используйте простые пароли\n" +
                "• Минимум 6 символов", cancellationToken: cancellationToken);
        }
    }

    public async Task HandleRegistrationStepAsync(Message message, CancellationToken cancellationToken)
    {
        var session = await _registrationStorage.GetRegistrationStateAsync(message.From.Id);
        
        if (session == null)
            return;
        
        try
        {
            switch (session.Step)
            {
                case RegistrationStep.Username:
                    await HandleUsernameStep(message, session, cancellationToken);
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
            await _registrationStorage.RemoveRegistrationStateAsync(message.From.Id);
            
            await _botService.SendMessageAsync(
                message.Chat.Id,
                "Произошла ошибка при регистрации. Попробуйте снова /register", 
                cancellationToken: cancellationToken);
        }
    }
    public Task<bool> IsUserInRegistrationAsync(long telegramUserId)
    {
        return _registrationStorage.IsUserInRegistrationAsync(telegramUserId);
    }
    private async Task HandleUsernameStep(Message message, UserRegistrationData session, CancellationToken cancellationToken)
    {
        var username = message.Text?.Trim();
        
        if (string.IsNullOrEmpty(username) || username.Length < 3)
        {
            await _botService.SendMessageAsync(
                message.Chat.Id,
                "Имя пользователя должно содержать минимум 3 символа. Попробуйте еще раз:",
                cancellationToken: cancellationToken);
            return;
        }

        // не занято ли имя пользователя
        var existingUser = await _userService.GetUserByUsernameAsync(username);
        if (existingUser != null)
        {
            await _botService.SendMessageAsync(
                message.Chat.Id, 
                "Это имя пользователя уже занято. Пожалуйста, выберите другое:",
                cancellationToken: cancellationToken);
            return;
        }

        session.Username = username;
        session.Step = RegistrationStep.Gender;

        await _registrationStorage.SetRegistrationStateAsync(session.TelegramUserId, session);
        await AskForGender(message.Chat.Id, cancellationToken);
    }
    
    private async Task HandleGenderStep(Message message, UserRegistrationData session, CancellationToken cancellationToken)
    {
        await _botService.SendMessageAsync(
            message.Chat.Id, 
            "Пожалуйста, выберите ваш пол используя кнопки выше:", 
            cancellationToken: cancellationToken);
    }

    private async Task HandlePasswordStep(Message message, UserRegistrationData session, CancellationToken cancellationToken)
    {
        var password = message.Text?.Trim();

        if (string.IsNullOrEmpty(password) || password.Length < 6)
        {
            await _botService.SendMessageAsync(
                message.Chat.Id, 
                "Пароль должен содержать минимум 6 символов. Попробуйте еще раз:",
                cancellationToken: cancellationToken);
            return;
        }

        session.Password = password;
        session.Step = RegistrationStep.Complete;

        await _registrationStorage.SetRegistrationStateAsync(session.TelegramUserId, session);
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
                
                await _telegramUserService.LinkToSystemUserAsync(session.TelegramUserId, userDto.Id);
                await _registrationStorage.RemoveRegistrationStateAsync(session.TelegramUserId);
                
                await _botService.SendMessageAsync(message.Chat.Id, $"""
                                                                     <b>Регистрация завершена успешно!</b>
                                         
                                                                     Вы успешно зарегистрированы в системе.
                                         
                                                                     <b>Ваши данные:</b>
                                                                        Имя пользователя: <code>{session.Username}</code>
                                                                        Пол: {GetGenderDisplayName(session.Gender)}
                                                                        ID: <code>{userDto.Id}</code>
                                         
                                                                     Теперь вы можете использовать все возможности системы!
                                                                     Используйте /profile для просмотра вашего профиля.
                                                                     """.Trim(), cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _registrationStorage.RemoveRegistrationStateAsync(session.TelegramUserId);
                
                await _botService.SendMessageAsync(
                    message.Chat.Id,
                    "Ошибка при создании пользователя. Попробуйте снова /register", 
                    cancellationToken: cancellationToken);
            }
        }
        else if (message.Text?.ToLower() == "нет")
        {
            await _registrationStorage.RemoveRegistrationStateAsync(session.TelegramUserId);
            await _botService.SendMessageAsync(
                message.Chat.Id, 
                "Регистрация отменена. Если хотите начать заново, используйте /register",
                cancellationToken: cancellationToken);
        }
        else
        {
            await _botService.SendMessageAsync(
                message.Chat.Id, 
                "Пожалуйста, ответьте 'Да' или 'Нет':", 
                cancellationToken: cancellationToken);
        }
    }

    private async Task AskForUsername(long chatId, CancellationToken cancellationToken)
    {
        await _botService.SendMessageAsync(chatId, 
            """
                <b>Регистрация в системе</b>

                Давайте создадим вашу учетную запись!

                <b>Шаг 1 из 3: Имя пользователя</b>

                Введите имя пользователя (от 3 до 20 символов):
                • Можно использовать буквы, цифры и символ _
                • Должно быть уникальным

                <i>Пример: ivan_petrov, anna2024, user_123</i>
                """.Trim(), cancellationToken: cancellationToken);
    }

    private async Task AskForGender(long chatId, CancellationToken cancellationToken)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Мужской", "/register gender:M"),
                InlineKeyboardButton.WithCallbackData("Женский", "/register gender:F")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Не указывать", "/register gender:U")
            }
        });

        await _botService.SendMessageAsync(chatId,
            """
                    <b>Шаг 2 из 3: Пол</b>

                    Выберите ваш пол (необязательно):
                """.Trim(), cancellationToken: cancellationToken);

        await _botService.SendMessageAsync(chatId, "Выберите пол:", keyboard, cancellationToken: cancellationToken);
    }

    private async Task ShowRegistrationSummary(long chatId, UserRegistrationData session, CancellationToken cancellationToken)
    {
        var summary = $"""
                       <b>Проверьте ваши данные:</b>
           
                       <b>Имя пользователя:</b> <code>{session.Username}</code>
                       <b>Пол:</b> {GetGenderDisplayName(session.Gender)}
           
                       <b>Всё верно?</b>
                       Ответьте 'Да' для завершения регистрации или 'Нет' для отмены.
                       """.Trim();

        await _botService.SendMessageAsync(chatId, summary, cancellationToken: cancellationToken);
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
    public string? Gender { get; set; }
    public string? Password { get; set; }
    public RegistrationStep Step { get; set; }
}

public enum RegistrationStep
{
    Username,
    Gender,
    Password,
    Complete
}