using HW1.Api.Domain.Contracts.Repositories;
using HW1.Api.Domain.Contracts.Security;
using HW1.Api.Domain.Contracts.Telegram;
using HW1.Api.Infrastructure.Database;
using HW1.Api.Infrastructure.Database.Repositories;
using HW1.Api.Infrastructure.Security;
using HW1.Api.Infrastructure.Telegram;
using HW1.Api.WebAPI.TelegramBot.Commands;
using Microsoft.EntityFrameworkCore;

namespace HW1.Api.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddScoped<IPasswordHasher, DefaultPasswordHasher>();
        services.AddScoped<IUserRepository, EfUserRepository>();
        
        services.Configure<TelegramBotConfiguration>(
            configuration.GetSection("TelegramBot"));
        
        services.AddScoped<IRegistrationStorage, RegistrationStorage>();

        
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("PostgresConnection"));
        }); 
        
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("RedisConnection");
        });

        services.AddGrpc(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaxReceiveMessageSize = 32 * 1024 * 1024;
            options.MaxSendMessageSize = 32 * 1024 * 1024;
        });
        
        return services;
    }
    
    public static IServiceCollection AddTelegramBotIntegration(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<ICommandHandler>()
            .AddClasses(classes => classes
                .AssignableTo<ICommandHandler>()
                .Where(type => !type.IsAbstract)
            )
            .AsSelfWithInterfaces() 
            .WithScopedLifetime());

        services.AddTransient<Func<IEnumerable<ICommandHandler>>>(serviceProvider => 
        {
            return () =>
            [
                serviceProvider.GetRequiredService<StartCommandHandler>(),
                serviceProvider.GetRequiredService<HelpCommandHandler>(),
                serviceProvider.GetRequiredService<StatsCommandHandler>(),
                serviceProvider.GetRequiredService<UsersCommandHandler>(),
                serviceProvider.GetRequiredService<RegisterCommandHandler>(),
                serviceProvider.GetRequiredService<ProfileCommandHandler>()
            ];
        });

        services.AddScoped<ITelegramUserService, TelegramUserService>();
        services.AddScoped<ITelegramBotService, TelegramBotService>();
        services.AddHostedService<TelegramBotBackgroundService>();

        return services;
    }
}