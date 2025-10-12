using HW1.Api.Domain.Contracts.Repositories;
using HW1.Api.Domain.Contracts.Security;
using HW1.Api.Domain.Contracts.Services;
using HW1.Api.Domain.Models;
using HW1.Api.Infrastructure.Database.Repositories;

namespace HW1.Api.Application.Services;

public class DataSeedService : IDataSeedService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DataSeedService> _logger;

    public DataSeedService(
        IUserRepository userRepository, 
        IPasswordHasher passwordHasher,
        ILogger<DataSeedService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<int> SeedTestUsersAsync(int count = 10)
    {
        var testUsers = GenerateTestUsers(count);
        
        foreach (var user in testUsers)
        {
            await _userRepository.AddUserAsync(user);
        }

        _logger.LogInformation("Добавлено {Count} тестовых пользователей", testUsers.Count);
        return testUsers.Count;
    }

    public async Task ClearTestDataAsync()
    {
        if (_userRepository is InMemoryUserRepository inMemoryRepo)
        {
            await inMemoryRepo.ClearAllAsync();
        }
        
        _logger.LogInformation("Тестовые данные очищены");
    }

    public async Task<bool> DatabaseIsEmptyAsync()
    {
        var users = await _userRepository.GetAllUsersAsync();
        return !users.Any();
    }

    private List<User> GenerateTestUsers(int count)
    {
        var users = new List<User>();
        var random = new Random();
        var genders = new[] { Gender.Male, Gender.Female };
        
        var firstNames = new[] { "Alex", "Maria", "John", "Anna", "Mike", "Elena", "David", "Sophia" };
        var lastNames = new[] { "Smith", "Johnson", "Brown", "Davis", "Wilson", "Taylor", "Clark", "Walker" };

        for (int i = 1; i <= count; i++)
        {
            var firstName = firstNames[random.Next(firstNames.Length)];
            var lastName = lastNames[random.Next(lastNames.Length)];
            var username = $"{firstName.ToLower()}.{lastName.ToLower()}{i}";
            
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Username = username,
                PasswordHash = _passwordHasher.HashPassword("Password"),
                Gender = genders[random.Next(genders.Length)],
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 365)),
                UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(0, 30))
            };
            
            users.Add(user);
        }

        return users;
    }
}