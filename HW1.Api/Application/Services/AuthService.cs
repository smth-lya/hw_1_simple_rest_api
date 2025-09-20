using HW1.Api.Application.DTOs;
using HW1.Api.Domain.Contracts.Repositories;
using HW1.Api.Domain.Contracts.Security;
using HW1.Api.Domain.Contracts.Services;

namespace HW1.Api.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResult> LoginAsync(string username, string password)
    {
        var user = await _userRepository.GetUserByUsernameAsync(username);
        if (user == null)
        {
            return new LoginResult(IsSuccess: false, Error: "Недействительные учетные данные");
        }

        var isPasswordValid = _passwordHasher.VerifyHashedPassword(password, user.PasswordHash);
        return !isPasswordValid 
            ? new LoginResult(IsSuccess: false, Error: "Недействительные учетные данные") 
            : new LoginResult(IsSuccess: true, UserId: user.Id, Username: user.Username);
    }
}