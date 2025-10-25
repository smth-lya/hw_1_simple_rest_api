using HW1.Api.Application.DTOs;
using HW1.Api.Domain.Contracts.Services;
using HW1.Api.Domain.Contracts.Telegram;
using HW1.Api.WebAPI.Models;
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
        ITelegramUserService telegramUserService)
        : base(botService, userService, telegramUserService) { }

    public override async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        if (!await ValidateUserAccessAsync(message.From.Id, cancellationToken))
        {
            await _botService.SendMessageAsync(
                message.Chat.Id,
                "❌ Доступ запрещен. Сначала выполните /start",
                cancellationToken);
            return;
        }

        var pageNumber = 1;
        var parts = message.Text?.Split(' ');
        if (parts?.Length > 1 && int.TryParse(parts[1], out var page))
        {
            pageNumber = Math.Max(1, page);
        }

        try
        {
            var pagination = new PaginationRequest { PageNumber = pageNumber, PageSize = 5 };
            var usersPage = await _userService.GetUsersPagedAsync(pagination);

            if (!usersPage.Items.Any())
            {
                await _botService.SendMessageAsync(
                    message.Chat.Id,
                    "❌ Пользователи не найдены",
                    cancellationToken);
                return;
            }

            var usersList = FormatUsersList(usersPage.Items);
            var navigation = FormatNavigation(usersPage, pageNumber);

            var messageText = $@"
👥 <b>Пользователи системы</b>

{usersList}

{navigation}
            ".Trim();

            var keyboard = CreateNavigationKeyboard(usersPage, pageNumber);

            await _botService.SendMessageAsync(
                message.Chat.Id,
                messageText,
                cancellationToken);
        }
        catch (Exception ex)
        {
            await _botService.SendMessageAsync(
                message.Chat.Id,
                "❌ Ошибка при получении списка пользователей",
                cancellationToken);
        }
    }

    private static string FormatUsersList(IEnumerable<UserDto> users)
    {
        return string.Join("\n\n", users.Select((user, index) => $@"
{(index + 1)}. <b>{user.Username}</b>
   📧 {user.Email}
   👤 {GetGenderEmoji(user.Gender)}
   📅 Зарегистрирован: {user.CreatedDate:dd.MM.yyyy}
   🔧 Роли: {string.Join(", ", user.Roles)}".Trim()));
    }

    private static string FormatNavigation(PagedResult<UserDto> page, int currentPage)
    {
        return $@"
📄 Страница {currentPage} из {page.TotalPages}
👤 Всего пользователей: {page.TotalCount}
        ".Trim();
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

        return buttons.Any() ? new InlineKeyboardMarkup(buttons) : null;
    }

    private static string GetGenderEmoji(string? gender) => gender?.ToUpper() switch
    {
        "M" => "👨",
        "F" => "👩",
        _ => "❓"
    };
}