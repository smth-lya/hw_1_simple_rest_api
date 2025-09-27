using HW1.Api.Application.DTOs;
using HW1.Api.Domain.Models;

namespace HW1.Api.Domain.Contracts.Services;

public interface IUserAnalyticsService
{
    Task<DateTime?> GetEarliestRegistrationDateAsync();
    Task<DateTime?> GetLatestRegistrationDateAsync();
    Task<IEnumerable<UserDto>> GetUsersSortedByUsernameAsync(bool ascending = true);
    Task<IEnumerable<UserDto>> GetUsersByGenderAsync(Gender gender);
    Task<int> GetTotalUsersCountAsync();
    Task<Dictionary<Gender, int>> GetUsersCountByGenderAsync();
}