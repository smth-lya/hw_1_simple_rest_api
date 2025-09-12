using HW1.Api;
using HW1.Api.Models;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
});

builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/auth/login", async (
    LoginRequest request,
    IUserRepository repository) =>
{
    var user = await repository.GetUserByUsernameAsync(request.Username);
    if (user == null)
        return Results.Unauthorized();

    if (user.PasswordHash != request.Password) // Подразумевается хеширвоание
        return Results.Unauthorized();
    
    return Results.Ok(user);
});

app.MapPost("/users/", async (
    RegisterRequest request, 
    IUserRepository repository) =>
{
    var userId = Guid.NewGuid();

    var existingUser = await repository.GetUserByUsernameAsync(request.Username);
    if (existingUser != null)
        return Results.BadRequest($"Username: {request.Username} уже существует");
    
    var newUser = new User()
    {
        Id = userId,
        Username = request.Username,
        PasswordHash = request.Password, // Предполагается хеширование
    };

    await repository.AddUserAsync(newUser);

    return Results.Created($"/user/{userId}", newUser);
});

app.MapGet("/users/{userId:guid}", async (
    Guid userId, 
    IUserRepository repository) =>
{
    var user = await repository.GetUserByIdAsync(userId);
    
    return user == null 
        ? Results.NotFound() 
        : Results.Ok(user);
});

app.MapPut("/users/{userId:guid}", async (
    Guid userId, 
    UpdateRequest request, 
    IUserRepository repository) =>
{
    var user = await repository.GetUserByIdAsync(userId);
    
    if (user == null)
        return Results.NotFound();

    var otherUser = await repository.GetUserByUsernameAsync(user.Username);
    if (otherUser != null && otherUser.Id != userId)
        return Results.BadRequest($"Username: {request.Username} уже существует");
    
    var updatedUser = new User()
    {
        Id = userId,
        Username = request.Username ?? user.Username,
        PasswordHash = request.Password ?? user.PasswordHash, // Предполагается хеширование
    };
    
    await repository.UpdateUserAsync(updatedUser);
    
    return Results.NoContent();
});

app.MapDelete("/users/{userId:guid}", async (
    Guid userId, 
    IUserRepository repository) =>
{
    var user = await repository.GetUserByIdAsync(userId);
    
    if (user == null)
        return Results.NotFound();
    
    await repository.DeleteUserAsync(userId);
    
    return Results.NoContent();
});

app.Run();