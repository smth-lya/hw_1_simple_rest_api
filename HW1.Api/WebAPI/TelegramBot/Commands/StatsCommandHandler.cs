using HW1.Api.Domain.Contracts.Services;
using HW1.Api.Domain.Contracts.Telegram;
using HW1.Api.Domain.Models;
using Telegram.Bot.Types;

namespace HW1.Api.WebAPI.TelegramBot.Commands;

public class StatsCommandHandler : BaseCommandHandler
{
    private readonly IUserAnalyticsService _analyticsService;

    public override string Command => "/stats";
    public override string Description => "Статистика системы";

    public StatsCommandHandler(
        ITelegramBotService botService,
        IUserService userService,
        ITelegramUserService telegramUserService,
        IUserAnalyticsService analyticsService)
        : base(botService, userService, telegramUserService)
    {
        _analyticsService = analyticsService;
    }

    public override async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        if (!await ValidateUserAccessAsync(message.From.Id, cancellationToken))
        {
            await _botService.SendMessageAsync(
                message.Chat.Id,
                "Доступ запрещен. Сначала выполните /start",
                cancellationToken);
            return;
        }

        try
        {
            var stats = await GetSystemStatsAsync();
            await _botService.SendMessageAsync(message.Chat.Id, stats, cancellationToken);
        }
        catch (Exception ex)
        {
            await _botService.SendMessageAsync(
                message.Chat.Id,
                "Ошибка при получении статистики",
                cancellationToken);
        }
    }

    private async Task<string> GetSystemStatsAsync()
    {
        var totalUsers = await _userService.GetTotalUsersCountAsync();
        var genderStats = await _analyticsService.GetUsersCountByGenderAsync();
        var earliestDate = await _analyticsService.GetEarliestRegistrationDateAsync();
        var latestDate = await _analyticsService.GetLatestRegistrationDateAsync();
        var telegramUsersCount = await _telegramUserService.GetActiveUsersCountAsync();

        var statsMessage = 
@$"""
📊 <b>Статистика системы</b>

<b>Пользователи системы:</b> {totalUsers}
<b>Пользователи бота:</b> {telegramUsersCount}

<b>По полу:</b>
    👨 Мужчины: {genderStats.GetValueOrDefault(Gender.Male, 0)}
    👩 Женщины: {genderStats.GetValueOrDefault(Gender.Female, 0)}
    ❓ Не указан: {genderStats.GetValueOrDefault(Gender.Undefined, 0)}

<b>Даты регистрации:</b>
    🏁 Первая: {earliestDate:dd.MM.yyyy}
    🎯 Последняя: {latestDate:dd.MM.yyyy}

<b>Обновлено:</b> {DateTime.Now:dd.MM.yyyy HH:mm}
""".Trim();

        return statsMessage;
    }
}