using HW.Application.Services;

namespace HW.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserAnalyticsService, UserAnalyticsService>();

        services.AddScoped<IDataSeedService, DataSeedService>();
        
        return services;
    }
}