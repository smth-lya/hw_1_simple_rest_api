namespace HW1.Api.Domain.Contracts.Services;

public interface IDataSeedService
{
    Task<int> SeedTestUsersAsync(int count = 10);
    Task ClearTestDataAsync();
    Task<bool> DatabaseIsEmptyAsync();
}