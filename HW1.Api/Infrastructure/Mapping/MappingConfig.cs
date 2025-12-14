using Mapster;
using HW1.Api.Domain.Models;
using HW1.Api.Application.DTOs;
using HW1.Api.Grpc;

namespace HW1.Api.Infrastructure.Mapping;

public static class MappingConfig
{
    public static void ConfigureMappings()
    {
        TypeAdapterConfig<UserDto, UserResponse>
            .NewConfig()
            .Map(dest => dest.Id, src => src.Id.ToString())
            .Map(dest => dest.Gender, src => src.Gender.ToString() ?? string.Empty)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"))
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss"));

        TypeAdapterConfig<TelegramUser, TelegramUserResponse>
            .NewConfig()
            .Map(dest => dest.TelegramUserId, src => src.TelegramUserId)
            .Map(dest => dest.Username, src => src.Username ?? string.Empty)
            .Map(dest => dest.LastName, src => src.LastName ?? string.Empty)
            .Map(dest => dest.RegisteredAt, src => src.RegisteredAt.ToString("yyyy-MM-ddTHH:mm:ss"))
            .Map(dest => dest.LastActivity, src => src.LastActivity.ToString("yyyy-MM-ddTHH:mm:ss"))
            .Map(dest => dest.SystemUserId, src => src.SystemUserId.ToString());
    }
}