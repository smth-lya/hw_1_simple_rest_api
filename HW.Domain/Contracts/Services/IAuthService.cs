namespace HW.Domain.Contracts.Services;

public interface IAuthService
{
    Task<AuthResult> AuthenticateAsync(string username, string password);
    
    
}