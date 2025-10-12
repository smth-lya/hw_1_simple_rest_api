using HW.Domain.Entities;
using HW.Domain.Models;

namespace HW.Domain.Contracts.Repositories;

public interface IUserRepository
{
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetUsersByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    
    Task AddUserAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ClearAllAsync();
}