using HW1.Api.Domain.Contracts.Services;
using HW1.Api.Domain.Contracts.Telegram;
using Telegram.Bot.Types;

namespace HW1.Api.WebAPI.TelegramBot.Commands;

public class HelpCommandHandler : BaseCommandHandler
{
    private readonly Func<IEnumerable<ICommandHandler>> _commandHandlersFactory;

    public override string Command => "/help";
    public override string Description => "Помощь и список команд";

    public HelpCommandHandler(
        ITelegramBotService botService,
        IUserService userService,
        ITelegramUserService telegramUserService,
        Func<IEnumerable<ICommandHandler>> commandHandlersFactory) 
        : base(botService, userService, telegramUserService)
    {
        _commandHandlersFactory = commandHandlersFactory;
    }

    public override async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        var commandText = message.Text?.Split(' ').LastOrDefault()?.ToLower();

        if (!string.IsNullOrEmpty(commandText) && commandText != "/help")
        {
            await ShowCommandHelpAsync(message.Chat.Id, commandText, cancellationToken);
        }
        else
        {
            await ShowGeneralHelpAsync(message.Chat.Id, cancellationToken);
        }
    }

    private async Task ShowGeneralHelpAsync(long chatId, CancellationToken cancellationToken)
    {
        var helpMessage = "📋 <b>Доступные команды:</b>\n\n";
        
        foreach (var handler in _commandHandlersFactory().OrderBy(h => h.Command)!)
        {
            helpMessage += $"<code>{handler.Command}</code> - {handler.Description}\n";
        }

        helpMessage += "\n💡 <i>Используйте /help [команда] для получения подробной информации</i>";

        await _botService.SendMessageAsync(chatId, helpMessage, cancellationToken: cancellationToken);
    }

    private async Task ShowCommandHelpAsync(long chatId, string command, CancellationToken cancellationToken)
    {
        var handler = _commandHandlersFactory().FirstOrDefault(h => 
            h.Command.Equals(command, StringComparison.OrdinalIgnoreCase));

        if (handler == null)
        {
            await _botService.SendMessageAsync(chatId, $"Команда <code>{command}</code> не найдена.\nИспользуйте /help для списка команд.", cancellationToken: cancellationToken);
            return;
        }

        var commandHelp = GetCommandSpecificHelp(handler.Command);
        await _botService.SendMessageAsync(chatId, commandHelp, cancellationToken: cancellationToken);
    }

    private static string GetCommandSpecificHelp(string command) => command.ToLower() switch
    {
        "/start" => """
                    <b>Команда /start</b>

                    Запускает бота и регистрирует пользователя в системе.

                    <b>Использование:</b>
                    <code>/start</code>

                    После выполнения команды вы получите приветственное сообщение и доступ ко всем функциям бота.
                            ",
                            "/stats" => @"
                    <b>Команда /stats</b>

                    Показывает статистику системы:
                    - Общее количество пользователей
                    - Активные пользователи
                    - Статистика по полу
                    - Даты регистрации

                    <b>Использование:</b>
                    <code>/stats</code>
                            ",
                            "/users" => @"
                    <b>Команда /users</b>

                    Показывает список пользователей системы с возможностью постраничного просмотра.

                    <b>Использование:</b>
                    <code>/users</code> - первая страница
                    <code>/users 2</code> - вторая страница
                    """,
        _ => $"Помощь по команде {command}\n\nОписание: {GetHandlerDescription(command)}"
    };

    private static string GetHandlerDescription(string command) => command.ToLower() switch
    {
        "/start" => "Запуск бота и регистрация пользователя",
        "/help" => "Помощь и список команд",
        "/stats" => "Статистика системы",
        "/users" => "Список пользователей",
        "/profile" => "Мой профиль",
        "/register" => "Регистрация в системе",
        _ => "Описание команды"
    };
}