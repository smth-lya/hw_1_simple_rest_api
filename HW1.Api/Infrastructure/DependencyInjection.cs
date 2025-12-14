using System.Net;
using Confluent.Kafka;
using HW1.Api.Domain.Contracts.Repositories;
using HW1.Api.Domain.Contracts.Security;
using HW1.Api.Domain.Contracts.Services.Kafka;
using HW1.Api.Domain.Contracts.Telegram;
using HW1.Api.Infrastructure.Database;
using HW1.Api.Infrastructure.Database.Repositories;
using HW1.Api.Infrastructure.Kafka;
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
        
        return services;
    }

    public static IServiceCollection AddKafka(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaSettings>(configuration.GetSection("Kafka"));
        
        services.AddSingleton<IProducer<Null, string>>(sp =>
        {
            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                ClientId = Dns.GetHostName(),
                Acks = Acks.Leader,
                MessageSendMaxRetries = 3,
                RetryBackoffMs = 1000
            };
            return new ProducerBuilder<Null, string>(config).Build();
        });

        services.AddSingleton<IConsumer<Ignore, string>>(sp =>
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                GroupId = $"telegram-bot-{Environment.MachineName}",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                EnableAutoOffsetStore = false
            };
            return new ConsumerBuilder<Ignore, string>(config).Build();
        });
        
        services.AddSingleton<KafkaProducerService>();
        services.AddSingleton<KafkaConsumerService>();
        services.AddSingleton<IAnalyticsService, KafkaAnalyticsService>();
        services.AddHostedService<KafkaBackgroundService>();
        
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