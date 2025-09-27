using HW1.Api.Application.DTOs;
using HW1.Api.Domain.Contracts.Repositories;
using HW1.Api.Domain.Contracts.Services;
using HW1.Api.Domain.Models;

namespace HW1.Api.Application.Services;

public class UserAnalyticsService : IUserAnalyticsService
{
    private readonly IUserRepository _userRepository;
    
    public UserAnalyticsService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<DateTime?> GetEarliestRegistrationDateAsync()
    {
        var users = await _userRepository.GetAllUsersAsync();
        return users.MinBy(u => u.CreatedAt)?.CreatedAt;
    }

    public async Task<DateTime?> GetLatestRegistrationDateAsync()
    {
        var users = await _userRepository.GetAllUsersAsync();
        return users.MaxBy(u => u.CreatedAt)?.CreatedAt;
    }

    public async Task<IEnumerable<UserDto>> GetUsersSortedByUsernameAsync(bool ascending = true)
    {
        var users = await _userRepository.GetAllUsersAsync();
        
        var sortedUsers = ascending 
            ? users.OrderBy(user => user.Username)
            : users.OrderByDescending(user => user.Username);
        
        return sortedUsers.Select(UserDto.FromUser);
    }
    
    public async Task<IEnumerable<UserDto>> GetUsersByGenderAsync(Gender gender)
    {
        var users = await _userRepository.GetAllUsersAsync();
        return users.Where(user => 
            user.Gender != null && 
            user.Gender == gender)
            .Select(UserDto.FromUser);
    }
    
    public async Task<int> GetTotalUsersCountAsync()
    {
        var users = await _userRepository.GetAllUsersAsync();
        return users.Count();
    }
    
    public async Task<Dictionary<Gender, int>> GetUsersCountByGenderAsync()
    {
        var users = await _userRepository.GetAllUsersAsync();
        
        return users
            .Where(user => user.Gender.HasValue)
            .GroupBy(user => user.Gender!.Value)
            .ToDictionary(group => group.Key, group => group.Count());
    }
}