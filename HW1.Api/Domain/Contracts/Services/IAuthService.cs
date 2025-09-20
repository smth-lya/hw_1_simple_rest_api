using HW1.Api.Application.DTOs;

namespace HW1.Api.Domain.Contracts.Services;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(string username, string password);
}