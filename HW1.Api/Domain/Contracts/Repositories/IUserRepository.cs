using HW1.Api.Domain.Models;
using HW1.Api.WebAPI.Models;

namespace HW1.Api.Domain.Contracts.Repositories;

public interface IUserRepository
{
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetUsersByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    
    Task<PagedResult<User>> GetUsersPagedAsync(PaginationRequest request);
    Task<PagedResult<User>> GetUsersPagedAsync(int pageNumber, int pageSize);
    
    Task AddUserAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateUserAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}