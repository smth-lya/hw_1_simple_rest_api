using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using HW1.Api.Domain.Contracts.Services;
using HW1.Api.Domain.Contracts.Telegram;
using HW1.Api.Grpc;
using Mapster;

namespace HW1.Api.Infrastructure.Grpc;

public class TelegramGrpcService : TelegramService.TelegramServiceBase
{
    private readonly ITelegramBotService _botService;
    private readonly ITelegramUserService _telegramUserService;
    private readonly IUserService _userService;
    private readonly ILogger<TelegramGrpcService> _logger;

    public TelegramGrpcService(
        ITelegramBotService botService,
        ITelegramUserService telegramUserService,
        IUserService userService,
        ILogger<TelegramGrpcService> logger)
    {
        _botService = botService;
        _telegramUserService = telegramUserService;
        _userService = userService;
        _logger = logger;
    }

    public override async Task<MessageResponse> SendMessage(SendMessageRequest request, ServerCallContext context)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["GrpcMethod"] = "SendMessage",
            ["ChatId"] = request.ChatId
        });

        try
        {
            await _botService.SendMessageAsync(
                request.ChatId,
                request.Message,
                cancellationToken: context.CancellationToken
            );

            _logger.LogInformation("Message sent via gRPC to chat {ChatId}", request.ChatId);
            
            return new MessageResponse
            {
                Success = true,
                MessageId = Guid.NewGuid().ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message via gRPC");
            
            return new MessageResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public override async Task<TelegramUserResponse> GetUserInfo(GetTelegramUserRequest request, ServerCallContext context)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["GrpcMethod"] = "GetUserInfo",
            ["TelegramUserId"] = request.TelegramUserId
        });

        try
        {
            var user = await _telegramUserService.GetUserAsync(request.TelegramUserId);
            if (user == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, 
                    $"Telegram user {request.TelegramUserId} not found"));
            }

            _logger.LogInformation("Telegram user info retrieved via gRPC");
            return user.Adapt<TelegramUserResponse>();
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Telegram user info via gRPC");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task BroadcastMessage(
        BroadcastMessageRequest request, 
        IServerStreamWriter<BroadcastProgress> responseStream, 
        ServerCallContext context)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["GrpcMethod"] = "BroadcastMessage",
            ["MessageLength"] = request.Message.Length
        });

        try
        {
            var activeUsers = await _telegramUserService.GetActiveUsersCountAsync();
            var totalUsers = await _userService.GetTotalUsersCountAsync();

            // пример рассылки
            
            for (int i = 0; i < 10; i++)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    break;

                var progress = new BroadcastProgress
                {
                    TotalUsers = totalUsers,
                    ProcessedUsers = (i + 1) * 100,
                    Successful = (i + 1) * 95,
                    Failed = (i + 1) * 5
                };

                await responseStream.WriteAsync(progress);
                await Task.Delay(1000); // имитация работы
            }

            _logger.LogInformation("Broadcast simulation completed via gRPC");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in BroadcastMessage gRPC stream");
            throw new RpcException(new Status(StatusCode.Internal, "Error during broadcast"));
        }
    }

    public override async Task<BotStatsResponse> GetBotStats(Empty request, ServerCallContext context)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["GrpcMethod"] = "GetBotStats"
        });

        try
        {
            var activeUsers = await _telegramUserService.GetActiveUsersCountAsync();
            
            var response = new BotStatsResponse
            {
                TotalMessagesToday = 150,
                ActiveUsers24H = activeUsers,
                ErrorRate = 0.5, 
                CommandsDistribution = 
                {
                    { "/start", 45 },
                    { "/help", 25 },
                    { "/profile", 15 },
                    { "/register", 10 },
                    { "/stats", 5 }
                }
            };

            _logger.LogInformation("Bot stats retrieved via gRPC");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bot stats via gRPC");
            throw new RpcException(new Status(StatusCode.Internal, "Error getting bot stats"));
        }
    }
}