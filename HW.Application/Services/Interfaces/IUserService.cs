using Ardalis.Result;
using HW.Application.DTOs.Requests;
using HW.Application.DTOs.Responses;

namespace HW.Application.Services.Interfaces;

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