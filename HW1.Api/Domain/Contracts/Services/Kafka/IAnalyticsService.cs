namespace HW1.Api.Domain.Contracts.Services.Kafka;

public interface IAnalyticsService
{
    Task PublishUserEventAsync(UserEvent userEvent, CancellationToken cancellationToken = default);
    Task PublishCommandEventAsync(CommandEvent commandEvent, CancellationToken cancellationToken = default);
    Task PublishRegistrationEventAsync(RegistrationEvent registrationEvent, CancellationToken cancellationToken = default);
    Task PublishErrorEventAsync(ErrorEvent errorEvent, CancellationToken cancellationToken = default);
    
    Task<AnalyticsSummary> GetDailySummaryAsync(DateTime date);
    Task<IEnumerable<UserActivity>> GetUserActivityAsync(long userId, TimeSpan period);
    Task<IEnumerable<TopCommand>> GetTopCommandsAsync(int topN = 10);
}