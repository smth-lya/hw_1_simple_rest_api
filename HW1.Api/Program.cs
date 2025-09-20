using FluentValidation;
using FluentValidation.AspNetCore;
using HW1.Api.Application;
using HW1.Api.Infrastructure;
using HW1.Api.WebAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddCustomSwaggerGen();

builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCustomSwaggerUi();
}

app.MapControllers();
app.Run();

// Тесты