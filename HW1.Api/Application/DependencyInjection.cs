using HW1.Api.Application.Services;
using HW1.Api.Domain.Contracts.Services;

namespace HW1.Api.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        
        return services;
    }
}