using HW.Application.DTOs;

namespace HW.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserDto> CreateUserAsync(string username, string password)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = _passwordHasher.HashPassword(password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddUserAsync(user);
        return UserDto.FromUser(user);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        return user != null ? UserDto.FromUser(user) : null;
    }

    public async Task<UserDto?> GetUserByUsernameAsync(string username)
    {
        var user = await _userRepository.GetUserByUsernameAsync(username);
        return user != null ? UserDto.FromUser(user) : null;
    }

    public async Task<IEnumerable<UserDto>> GetUsersByDateRangeAsync(DateTime? fromDate, DateTime? toDate)
    {
        var users = await _userRepository.GetUsersByDateRangeAsync(fromDate, toDate);
        return users.Select(UserDto.FromUser);
    }
    
    public async Task<PagedResult<UserDto>> GetUsersPagedAsync(PaginationRequest request)
    {
        var pagedResult = await _userRepository.GetUsersPagedAsync(request);
        
        return new PagedResult<UserDto>
        {
            Items = pagedResult.Items.Select(UserDto.FromUser),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize
        };    }

    public async Task<PagedResult<UserDto>> GetUsersPagedAsync(int pageNumber, int pageSize)
    {
        var pagedResult = await _userRepository.GetUsersPagedAsync(pageNumber, pageSize);
        
        return new PagedResult<UserDto>
        {
            Items = pagedResult.Items.Select(UserDto.FromUser),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize
        };
    }

    public async Task UpdateUserAsync(Guid id, string? username, string? password)
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
            return; 

        var updatedUser = new User()
        {
            Id = id,
            Username = username ?? user.Username,
            PasswordHash = password == null ? user.PasswordHash : _passwordHasher.HashPassword(password),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        await _userRepository.UpdateUserAsync(updatedUser);
    }

    public async Task DeleteUserAsync(Guid id)
    {
        await _userRepository.DeleteUserAsync(id);
    }
}