namespace HW.Domain.Contracts.Services;

public interface IUserService
{
    Task<Result> RegisterUserAsync(Username username, Password password);
    Task<Result> AuthenticateUserAsync(UserId id);
    Task<Result> ResetPasswordAsync(User user,Password password);
    Task<UserDto> CreateUserAsync(string username, string password);
    Task<UserDto?> GetUserByIdAsync(Guid id);
    Task<UserDto?> GetUserByUsernameAsync(string username);
    Task<IEnumerable<UserDto>> GetUsersByDateRangeAsync(DateTime? fromDate, DateTime? toDate);
    
    Task<PagedResult<UserDto>> GetUsersPagedAsync(PaginationRequest request);
    Task<PagedResult<UserDto>> GetUsersPagedAsync(int pageNumber, int pageSize);
    
    Task UpdateUserAsync(Guid id, string? username, string? password);
    Task DeleteUserAsync(Guid id);
}