using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using HW1.Api.Domain.Contracts.Services;
using HW1.Api.Grpc;
using HW1.Api.WebAPI.Models;
using Mapster;

namespace HW1.Api.Infrastructure.Grpc;

public class UserGrpcService : UserService.UserServiceBase
{
    private readonly IUserService _userService;
    private readonly IUserAnalyticsService _analyticsService;
    private readonly ILogger<UserGrpcService> _logger;

    public UserGrpcService(
        IUserService userService,
        IUserAnalyticsService analyticsService,
        ILogger<UserGrpcService> logger)
    {
        _userService = userService;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public override async Task<UserResponse> GetUser(GetUserRequest request, ServerCallContext context)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["GrpcMethod"] = "GetUser",
            ["UserId"] = request.UserId
        });

        try
        {
            if (!Guid.TryParse(request.UserId, out var userId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format"));
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"User {request.UserId} not found"));
            }

            _logger.LogInformation("User retrieved successfully via gRPC");
            return user.Adapt<UserResponse>();
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetUser gRPC method");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<UserResponse> CreateUser(CreateUserRequest request, ServerCallContext context)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["GrpcMethod"] = "CreateUser",
            ["Username"] = request.Username
        });

        try
        {
            var userDto = await _userService.CreateUserAsync(
                request.Username,
                request.Password
            );

            _logger.LogInformation("User created successfully via gRPC: {Username}", request.Username);
            return userDto.Adapt<UserResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateUser gRPC method");
            throw new RpcException(new Status(StatusCode.Internal, $"Failed to create user: {ex.Message}"));
        }
    }

    public override async Task GetUsers(GetUsersRequest request, IServerStreamWriter<UserResponse> responseStream, ServerCallContext context)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["GrpcMethod"] = "GetUsers",
            ["Page"] = request.PageNumber,
            ["PageSize"] = request.PageSize
        });

        try
        {
            var pagination = new PaginationRequest
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            var usersPage = await _userService.GetUsersPagedAsync(pagination);

            foreach (var user in usersPage.Items)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    break;

                await responseStream.WriteAsync(user.Adapt<UserResponse>());
                await Task.Delay(10); // Небольшая пауза для демонстрации стриминга
            }

            _logger.LogInformation("Streamed {Count} users via gRPC", usersPage.Items.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetUsers gRPC stream");
            throw new RpcException(new Status(StatusCode.Internal, "Error streaming users"));
        }
    }

    public override async Task<UserStatsResponse> GetUserStats(Empty request, ServerCallContext context)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["GrpcMethod"] = "GetUserStats"
        });

        try
        {
            var totalUsers = await _userService.GetTotalUsersCountAsync();
            var genderStats = await _analyticsService.GetUsersCountByGenderAsync();
            var earliestDate = await _analyticsService.GetEarliestRegistrationDateAsync();
            var latestDate = await _analyticsService.GetLatestRegistrationDateAsync();

            var response = new UserStatsResponse
            {
                TotalUsers = totalUsers,
                GenderStats = { 
                    genderStats.ToDictionary(
                        g => g.Key.ToString(), 
                        g => g.Value) 
                },
                EarliestRegistration = earliestDate.Value.ToString("yyyy-MM-dd"),
                LatestRegistration = latestDate.Value.ToString("yyyy-MM-dd")
            };

            _logger.LogInformation("User stats retrieved via gRPC");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetUserStats gRPC method");
            throw new RpcException(new Status(StatusCode.Internal, "Error getting user stats"));
        }
    }
}