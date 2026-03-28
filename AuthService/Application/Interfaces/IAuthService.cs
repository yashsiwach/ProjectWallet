using AuthService.Domain.Models;
using AuthService.DTOs;

namespace AuthService.Application.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest req);
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest req);
    Task<ApiResponse<string>> SubmitKycAsync(Guid userId, KycRequest req);
    Task<ApiResponse<ProfileResponse>> GetProfileAsync(Guid userId);
    Task<bool> UpdateUserStatusAsync(Guid userId, string status);
    Task<User?> GetUserByEmailAsync(string email);
}
