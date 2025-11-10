using HW1.Api.Application.DTOs;
using HW1.Api.Domain.Contracts.Services;
using HW1.Api.Domain.Contracts.Telegram;
using HW1.Api.Domain.Models;
using HW1.Api.WebAPI.Models;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace HW1.Api.WebAPI.TelegramBot.Commands;

public class UsersCommandHandler : BaseCommandHandler
{
    public override string Command => "/users";
    public override string Description => "Список пользователей";

    public UsersCommandHandler(
        ITelegramBotService botService,
        IUserService userService,
        ITelegramUserService telegramUserService,
        ILogger<UsersCommandHandler> logger)
        : base(botService, userService, telegramUserService, logger) { }

    public override async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        using var activity = BeginCommandScope(message);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Processing users command from user {UserId}", message.From?.Id);

            if (!await ValidateUserAccessAsync(message.From.Id, cancellationToken))
            {
                _logger.LogWarning("User {UserId} access denied for users command", message.From?.Id);
                await _botService.SendMessageAsync(
                    message.Chat.Id, 
                    "Доступ запрещен. Сначала выполните /start",
                    cancellationToken: cancellationToken);
                return;
            }

            var pageNumber = 1;
            var parts = message.Text?.Split(' ');
            if (parts?.Length > 1 && int.TryParse(parts[1], out var page))
            {
                pageNumber = Math.Max(1, page);
            }

            _logger.LogDebug("Fetching users page {PageNumber} for user {UserId}", pageNumber, message.From?.Id);

            var pagination = new PaginationRequest { PageNumber = pageNumber, PageSize = 5 };
            var usersPage = await _userService.GetUsersPagedAsync(pagination);

            if (!usersPage.Items.Any())
            {
                _logger.LogInformation("No users found for page {PageNumber}", pageNumber);
                await _botService.SendMessageAsync(
                    message.Chat.Id, 
                    "Пользователи не найдены", 
                    cancellationToken: cancellationToken);
                return;
            }

            var usersList = FormatUsersList(usersPage.Items);
            var navigation = FormatNavigation(usersPage, pageNumber);

            var messageText = $"""
                               <b>Пользователи системы</b>

                               {usersList}

                               {navigation}
                               """.Trim();

            var keyboard = CreateNavigationKeyboard(usersPage, pageNumber);

            await _botService.SendMessageAsync(message.Chat.Id, messageText, keyboard, cancellationToken: cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "Users command completed successfully in {ElapsedMs}ms for user {UserId}. Page {PageNumber}/{TotalPages}, Items: {ItemCount}", 
                stopwatch.ElapsedMilliseconds, message.From?.Id, pageNumber, usersPage.TotalPages, usersPage.Items.Count());
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex, 
                "Error processing users command after {ElapsedMs}ms for user {UserId}", 
                stopwatch.ElapsedMilliseconds, message.From?.Id);
                
            await _botService.SendMessageAsync(
                message.Chat.Id,
                "Ошибка при получении списка пользователей", 
                cancellationToken: cancellationToken);
        }
    }

    private static string FormatUsersList(IEnumerable<UserDto> users)
    {
        return string.Join("\n\n", users.Select((user, index) => $@"
        {(index + 1)}. <b>{user.Username}</b>
           {GetGenderEmoji(user.Gender)}
           Зарегистрирован: {user.CreatedAt:dd.MM.yyyy}
           Роли: {string.Join(", ", ["User"])}".Trim()));
    }

    private static string FormatNavigation(PagedResult<UserDto> page, int currentPage)
    {
        return $"""
                Страница {currentPage} из {page.TotalPages}
                Всего пользователей: {page.TotalCount}
                """.Trim();
    }

    private static InlineKeyboardMarkup? CreateNavigationKeyboard(PagedResult<UserDto> page, int currentPage)
    {
        var buttons = new List<InlineKeyboardButton>();

        if (page.HasPreviousPage)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"/users {currentPage - 1}"));
        }

        if (page.HasNextPage)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData("Вперед ➡️", $"/users {currentPage + 1}"));
        }

        return buttons.Count != 0 ? new InlineKeyboardMarkup(buttons) : null;
    }

    private static string GetGenderEmoji(Gender? gender) => gender switch
    {
        Gender.Male => "👨",
        Gender.Female => "👩",
        _ => "❓"
    };
}