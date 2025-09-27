using HW1.Api.Application.DTOs;
using HW1.Api.Domain.Models;
using HW1.Api.WebAPI.Models;

namespace HW1.Api.Domain.Contracts.Services;

public interface IUserService
{
    Task<UserDto> CreateUserAsync(string username, string password);
    Task<UserDto?> GetUserByIdAsync(Guid id);
    Task<UserDto?> GetUserByUsernameAsync(string username);
    Task<IEnumerable<UserDto>> GetUsersByDateRangeAsync(DateTime? fromDate, DateTime? toDate);
    
    Task<PagedResult<UserDto>> GetUsersPagedAsync(PaginationRequest request);
    Task<PagedResult<UserDto>> GetUsersPagedAsync(int pageNumber, int pageSize);
    
    Task UpdateUserAsync(Guid id, string? username, string? password);
    Task DeleteUserAsync(Guid id);
}