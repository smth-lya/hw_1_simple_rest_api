using FluentValidation;
using FluentValidation.AspNetCore;
using HW1.Api.Application;
using HW1.Api.Domain.Contracts.Telegram;
using HW1.Api.Infrastructure;
using HW1.Api.Infrastructure.Database;
using HW1.Api.Infrastructure.Telegram;
using HW1.Api.WebAPI.Extensions;
using HW1.Api.WebAPI.TelegramBot.Commands;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddCustomSwaggerGen();

builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<ITelegramUserService, TelegramUserService>();

builder.Services.AddScoped<ICommandHandler, StartCommandHandler>();
builder.Services.AddScoped<ICommandHandler, HelpCommandHandler>();
builder.Services.AddScoped<ICommandHandler, StatsCommandHandler>();
builder.Services.AddScoped<ICommandHandler, UsersCommandHandler>();
builder.Services.AddScoped<ICommandHandler, RegisterCommandHandler>();
builder.Services.AddScoped<ICommandHandler, ProfileCommandHandler>();

builder.Services.AddScoped<RegisterCommandHandler, RegisterCommandHandler>();

builder.Services.AddTransient<Func<IEnumerable<ICommandHandler>>>(serviceProvider => 
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

builder.Services.AddScoped<ITelegramBotService, TelegramBotService>();
builder.Services.AddHostedService<TelegramBotBackgroundService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCustomSwaggerUi();
    MigrateDatabase(app);
}

app.MapControllers();
app.Run();

// Тесты

static void MigrateDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    if (dbContext.Database.GetPendingMigrations().Any())
    {
        dbContext.Database.Migrate();
    }
}