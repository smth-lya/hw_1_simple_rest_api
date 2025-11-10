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
        Func<IEnumerable<ICommandHandler>> commandHandlersFactory,
        ILogger<HelpCommandHandler> logger) 
        : base(botService, userService, telegramUserService, logger)
    {
        _commandHandlersFactory = commandHandlersFactory;
    }

    public override async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        using var activity = BeginCommandScope(message);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Processing help command from user {UserId}", message.From?.Id);

            var parts = message.Text?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts == null || parts.Length == 0)
                return;

            var command = parts[0].ToLower();
            var argument = parts.Length > 1 ? parts[1].ToLower() : null;

            if (command == "/help" && !string.IsNullOrEmpty(argument))
            {
                _logger.LogDebug("Showing specific help for command: {CommandArgument}", argument);
                await ShowCommandHelpAsync(message.Chat.Id, argument, cancellationToken);
            }
            else
            {
                _logger.LogDebug("Showing general help with all commands");
                await ShowGeneralHelpAsync(message.Chat.Id, cancellationToken);
            }

            stopwatch.Stop();
            _logger.LogInformation("Help command completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing help command after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private async Task ShowGeneralHelpAsync(long chatId, CancellationToken cancellationToken)
    {
        var handlers = _commandHandlersFactory().OrderBy(h => h.Command).ToList();
        
        _logger.LogInformation("Showing general help with {CommandCount} commands", handlers.Count);

        var helpMessage = "📋 <b>Доступные команды:</b>\n\n";
        
        foreach (var handler in handlers)
        {
            helpMessage += $"{handler.Command} - {handler.Description}\n";
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
            _logger.LogWarning("Requested help for unknown command: {UnknownCommand}", command);
            await _botService.SendMessageAsync(
                chatId, 
                $"Команда {command} не найдена.\nИспользуйте /help для списка команд.", 
                cancellationToken: cancellationToken);
            return;
        }

        _logger.LogDebug("Showing specific help for command: {CommandName}", handler.Command);
        
        var commandHelp = GetCommandSpecificHelp(handler.Command);
        await _botService.SendMessageAsync(chatId, commandHelp, cancellationToken: cancellationToken);
    }

    private static string GetCommandSpecificHelp(string command) => command.ToLower() switch
    {
        "/start" => """
                    <b>Команда /start</b>

                    Запускает бота и регистрирует пользователя в системе.

                    <b>Использование:</b>
                    /start

                    После выполнения команды вы получите приветственное сообщение и доступ ко всем функциям бота.
                    """,
        "/stats" => """
                    <b>Команда /stats</b>

                    Показывает статистику системы:
                    - Общее количество пользователей
                    - Активные пользователи
                    - Статистика по полу
                    - Даты регистрации

                    <b>Использование:</b>
                    /stats
                    """,
        "/users" => """
                    <b>Команда /users</b>

                    Показывает список пользователей системы с возможностью постраничного просмотра.

                    <b>Использование:</b>
                    /users - первая страница
                    /users 2 - вторая страница
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