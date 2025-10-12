using HW.Application.DTOs.Requests;
using HW.Application.DTOs.Responses;

namespace HW.Application.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResult> AuthenticateAsync(LoginRequest request);
    
    
}