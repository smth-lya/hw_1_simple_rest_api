using HW1.Api.Application.DTOs;

namespace HW1.Api.Domain.Contracts.Services;

public interface IUserService
{
    Task<UserDto> CreateUserAsync(string username, string password);
    Task<UserDto?> GetUserByIdAsync(Guid id);
    Task<UserDto?> GetUserByUsernameAsync(string username);
    Task<IEnumerable<UserDto>> GetUsersByDateRangeAsync(DateTime? fromDate, DateTime? toDate);
    Task UpdateUserAsync(Guid id, string? username, string? password);
    Task DeleteUserAsync(Guid id);
}