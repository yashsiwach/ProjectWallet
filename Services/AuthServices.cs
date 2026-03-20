using AuthService.Data;
using AuthService.DTOs;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthService.Services
{
    public class AuthServices
    {
        private readonly AuthDbContext _db;
        private readonly IConfiguration _config;
        public AuthServices(AuthDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }
        public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest req)
        {
            //will chekc if email exists
            var emailExists = await _db.Users.AnyAsync(u => u.Email == req.Email);
            if (emailExists)
            {
                return ApiResponse<AuthResponse>.Fail("Email already in use");
            }
            //will check if phone number exists
            var phoneExists = await _db.Users.AnyAsync(u => u.PhoneNumber == req.PhoneNumber);
            if (phoneExists)
            {
                return ApiResponse<AuthResponse>.Fail("Phone number already in use");
            }
            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = req.FullName.Trim(),
                Email = req.Email.ToLower().Trim(),
                PhoneNumber = req.PhoneNumber.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                Role = "User",
                //only kyc in pending 
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

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
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLower().Trim());
            if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            {
                return ApiResponse<AuthResponse>.Fail("Invalid email or password");
            }
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

        public async Task<ApiResponse<string>>SubmitKycAsync(Guid userId, KycRequest req)
        {
            var user = await _db.Users.Include(u => u.KycDocument).FirstOrDefaultAsync(u => u.Id == userId);
            //found no user
            if (user == null)
            {
                return ApiResponse<string>.Fail("User not found");
            }

            if (user.KycDocument?.Status == "Approved")
            {
                return ApiResponse<string>.Fail("KYC already approved");
            }

            if (user.KycDocument?.Status == "Pending")
            {
                return ApiResponse<string>.Fail("Your KYC is already submitted and under review.");
            }
            //replace the KycDocumnet if exists 
            if (user.KycDocument != null)
            {
                _db.KycDocuments.Remove(user.KycDocument);
                await _db.SaveChangesAsync();
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

            _db.KycDocuments.Add(kyc);
            await _db.SaveChangesAsync();
            return ApiResponse<string>.Ok("KYC submitted successfully", null);

        }

        public async Task<ApiResponse<ProfileResponse>> GetProfileAsync(Guid userId)
        {
            var user = await _db.Users.Include(u => u.KycDocument).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return ApiResponse<ProfileResponse>.Fail("User not found");
            }

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

        private string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("FullName", user.FullName),
                new Claim("Email", user.Email),
                new Claim("Role", user.Role),
                new Claim("Status", user.Status)
            };

            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower().Trim()&& u.Status == "Active");
        }
    }
}