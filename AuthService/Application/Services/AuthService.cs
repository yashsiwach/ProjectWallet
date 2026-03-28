using AuthService.Application.Interfaces;
using AuthService.Domain.Models;
using AuthService.DTOs;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthService.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IKycRepository _kycRepository;
    private readonly IConfiguration _config;
    private readonly IRabbitMqPublisher _rabbitMq;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IKycRepository kycRepository,
        IConfiguration config,
        IRabbitMqPublisher rabbitMq,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _kycRepository = kycRepository;
        _config = config;
        _rabbitMq = rabbitMq;
        _logger = logger;
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest req)
    {
        var emailExists = await _userRepository.EmailExistsAsync(req.Email.ToLower().Trim());
        if (emailExists)
            return ApiResponse<AuthResponse>.Fail("Email already in use");

        var phoneExists = await _userRepository.PhoneExistsAsync(req.PhoneNumber.Trim());
        if (phoneExists)
            return ApiResponse<AuthResponse>.Fail("Phone number already in use");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = req.FullName.Trim(),
            Email = req.Email.ToLower().Trim(),
            PhoneNumber = req.PhoneNumber.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = "User",
            Status = "Pending",
            CreatedAt = DateTime.Now
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        _logger.LogInformation("User registered: {Email}", user.Email);

        var token = GenerateToken(user);
        return ApiResponse<AuthResponse>.Ok("Registration successful", new AuthResponse
        {
            Token = token,
            UserId = user.Id.ToString(),
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            Status = user.Status
        });
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest req)
    {
        var user = await _userRepository.FindByEmailAsync(req.Email.ToLower().Trim());
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return ApiResponse<AuthResponse>.Fail("Invalid email or password");

        _logger.LogInformation("User logged in: {Email}", user.Email);

        var token = GenerateToken(user);
        return ApiResponse<AuthResponse>.Ok("Login successful", new AuthResponse
        {
            Token = token,
            UserId = user.Id.ToString(),
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            Status = user.Status
        });
    }

    public async Task<ApiResponse<string>> SubmitKycAsync(Guid userId, KycRequest req)
    {
        var user = await _userRepository.FindByIdWithKycAsync(userId);
        if (user == null)
            return ApiResponse<string>.Fail("User not found");

        if (user.KycDocument?.Status == "Approved")
            return ApiResponse<string>.Fail("KYC already approved");

        if (user.KycDocument?.Status == "Pending")
            return ApiResponse<string>.Fail("Your KYC is already submitted and under review.");

        if (user.KycDocument != null)
        {
            await _kycRepository.RemoveAsync(user.KycDocument);
            await _kycRepository.SaveChangesAsync();
        }

        var kyc = new KycDocument
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DocumentType = req.DocumentType.Trim(),
            DocumentNumber = req.DocumentNumber.Trim(),
            Status = "Pending",
            SubmittedAt = DateTime.Now
        };

        await _kycRepository.AddAsync(kyc);
        await _kycRepository.SaveChangesAsync();

        _rabbitMq.Publish("kyc_submitted", new
        {
            UserId = userId,
            UserFullName = user.FullName,
            UserEmail = user.Email,
            DocumentType = req.DocumentType,
            DocumentNumber = req.DocumentNumber,
            SubmittedAt = kyc.SubmittedAt
        });

        _logger.LogInformation("KYC submitted for UserId: {UserId}", userId);

        return ApiResponse<string>.Ok("KYC submitted. Awaiting admin review.", "");
    }

    public async Task<ApiResponse<ProfileResponse>> GetProfileAsync(Guid userId)
    {
        var user = await _userRepository.FindByIdWithKycAsync(userId);
        if (user == null)
            return ApiResponse<ProfileResponse>.Fail("User not found");

        KycInfo? kycInfo = null;
        if (user.KycDocument != null)
        {
            kycInfo = new KycInfo
            {
                DocumentType = user.KycDocument.DocumentType,
                DocumentNumber = user.KycDocument.DocumentNumber,
                Status = user.KycDocument.Status,
                AdminNote = user.KycDocument.AdminNote,
                SubmittedAt = user.KycDocument.SubmittedAt,
                ReviewedAt = user.KycDocument.ReviewedAt
            };
        }

        return ApiResponse<ProfileResponse>.Ok("Profile retrieved successfully", new ProfileResponse
        {
            UserId = user.Id.ToString(),
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            Status = user.Status,
            Kyc = kycInfo
        });
    }

    public async Task<bool> UpdateUserStatusAsync(Guid userId, string status)
    {
        var user = await _userRepository.FindByIdAsync(userId);
        if (user == null) return false;

        user.Status = status;
        await _userRepository.SaveChangesAsync();

        _logger.LogInformation("User {UserId} status updated to {Status}", userId, status);
        return true;
    }

    public Task<User?> GetUserByEmailAsync(string email) =>
        _userRepository.FindActiveByEmailAsync(email.ToLower().Trim());

    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("FullName", user.FullName),
            new Claim("Email", user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("Status", user.Status)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
