using FluentValidation;
using FluentValidation.AspNetCore;
using HW1.Api.Application;
using HW1.Api.Domain.Contracts.Telegram;
using HW1.Api.Infrastructure;
using HW1.Api.Infrastructure.Database;
using HW1.Api.Infrastructure.Grpc;
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

builder.Services.AddTelegramBotIntegration();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCustomSwaggerUi();
    MigrateDatabase(app);
}

app.MapControllers();

app.MapGrpcService<UserGrpcService>();
app.MapGrpcService<TelegramGrpcService>();

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