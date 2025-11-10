using HW1.Api.Domain.Contracts.Services;
using HW1.Api.Domain.Contracts.Telegram;
using HW1.Api.Domain.Models;
using Microsoft.Extensions.Logging;
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
        IUserAnalyticsService analyticsService,
        ILogger<StatsCommandHandler> logger)
        : base(botService, userService, telegramUserService, logger)
    {
        _analyticsService = analyticsService;
    }

    public override async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        using var activity = BeginCommandScope(message);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Processing stats command from user {UserId}", message.From?.Id);

            if (!await ValidateUserAccessAsync(message.From.Id, cancellationToken))
            {
                _logger.LogWarning("User {UserId} access denied for stats command", message.From?.Id);
                await _botService.SendMessageAsync(
                    message.Chat.Id, 
                    "Доступ запрещен. Сначала выполните /start",
                    cancellationToken: cancellationToken);
                return;
            }

            var stats = await GetSystemStatsAsync();
            await _botService.SendMessageAsync(message.Chat.Id, stats, cancellationToken: cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "Stats command completed successfully in {ElapsedMs}ms for user {UserId}", 
                stopwatch.ElapsedMilliseconds, message.From?.Id);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex, 
                "Error processing stats command after {ElapsedMs}ms for user {UserId}", 
                stopwatch.ElapsedMilliseconds, message.From?.Id);
                
            await _botService.SendMessageAsync(
                message.Chat.Id, 
                "Ошибка при получении статистики", 
                cancellationToken: cancellationToken);
        }
    }

    private async Task<string> GetSystemStatsAsync()
    {
        _logger.LogDebug("Fetching system statistics");

        var totalUsers = await _userService.GetTotalUsersCountAsync();
        var genderStats = await _analyticsService.GetUsersCountByGenderAsync();
        var earliestDate = await _analyticsService.GetEarliestRegistrationDateAsync();
        var latestDate = await _analyticsService.GetLatestRegistrationDateAsync();
        var telegramUsersCount = await _telegramUserService.GetActiveUsersCountAsync();

        _logger.LogDebug(
            "Statistics fetched: TotalUsers={TotalUsers}, TelegramUsers={TelegramUsers}, GenderStats={GenderStats}", 
            totalUsers, telegramUsersCount, string.Join(",", genderStats.Select(g => $"{g.Key}:{g.Value}")));

        var statsMessage = 
            $"""
                 <b>Статистика системы</b>

                 <b>Пользователи системы:</b> {totalUsers}
                 <b>Пользователи бота:</b> {telegramUsersCount}

                 <b>По полу:</b>
                     Мужчины: {genderStats.GetValueOrDefault(Gender.Male, 0)}
                     Женщины: {genderStats.GetValueOrDefault(Gender.Female, 0)}
                     Не указан: {genderStats.GetValueOrDefault(Gender.Undefined, 0)}

                 <b>Даты регистрации:</b>
                     Первая: {earliestDate:dd.MM.yyyy}
                     Последняя: {latestDate:dd.MM.yyyy}

                 <b>Обновлено:</b> {DateTime.Now:dd.MM.yyyy HH:mm}
                 """.Trim();

        return statsMessage;
    }
}