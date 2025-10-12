using HW.Application.DTOs.Responses;
using HW.Domain.Models;

namespace HW.Application.Services.Interfaces;

public interface IUserAnalyticsService
{
    Task<DateTime?> GetEarliestRegistrationDateAsync();
    Task<DateTime?> GetLatestRegistrationDateAsync();
    Task<IEnumerable<UserDto>> GetUsersSortedByUsernameAsync(bool ascending = true);
    Task<IEnumerable<UserDto>> GetUsersByGenderAsync(Gender gender);
    Task<int> GetTotalUsersCountAsync();
    Task<Dictionary<Gender, int>> GetUsersCountByGenderAsync();
}