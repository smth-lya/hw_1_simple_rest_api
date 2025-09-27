using System.Collections.Concurrent;
using HW1.Api.Domain.Contracts.Repositories;
using HW1.Api.Domain.Models;
using HW1.Api.WebAPI.Models;

namespace HW1.Api.Infrastructure.Database.Repositories;

public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();
    private readonly ILogger<InMemoryUserRepository> _logger;
    
    public InMemoryUserRepository(ILogger<InMemoryUserRepository> logger)
    {
        _logger = logger;
    }
    
    public Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var user = _users.GetValueOrDefault(userId);

        return Task.FromResult(user);
    }

    public Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var user = _users.Values.FirstOrDefault(u => u.Username == username);
        
        return Task.FromResult(user);
    }

    public Task<IEnumerable<User>> GetUsersByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var query = _users.Values.AsEnumerable();

        if (fromDate.HasValue)
        {
            query = query.Where(u => u.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(u => u.CreatedAt <= toDate.Value);
        }
        
        var result = query.OrderBy(u => u.CreatedAt).ToList();
        
        return Task.FromResult(result.AsEnumerable());
    }

    public Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        return Task.FromResult(_users.Values.AsEnumerable());
    }

    public async Task<PagedResult<User>> GetUsersPagedAsync(PaginationRequest request)
    {
        return await GetUsersPagedAsync(request.PageNumber, request.PageSize);
    }

    public async Task<PagedResult<User>> GetUsersPagedAsync(int pageNumber, int pageSize)
    {
        await Task.CompletedTask;
        
        var items = _users.Values
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<User>()
        {
            Items = items,
            TotalCount = _users.Count,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public Task AddUserAsync(User user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        
        var added = _users.TryAdd(user.Id, user);
        if (!added)
            throw new InvalidOperationException($"Пользователь с ID {user.Id} уже существует");

        return Task.CompletedTask;
    }

    public Task UpdateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);
        
        var existingUser = _users.GetValueOrDefault(user.Id);
        if (existingUser == null)
            throw new InvalidOperationException($"Пользователь с ID {user.Id} не существует");
        
        _users[user.Id] = user;
        
        return Task.CompletedTask;
    }

    public Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var removed = _users.TryRemove(userId, out _);
        if (!removed)
            throw new KeyNotFoundException($"Пользователь с ID {userId} не найден");

        return Task.CompletedTask;
    }
}