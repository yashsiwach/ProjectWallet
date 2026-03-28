using AdminService.Application.Interfaces;
using AdminService.Domain.Models;
using AdminService.DTOs;

namespace AdminService.Application.Services;

public class AdminService : IAdminService
{
    private readonly IKycReviewRepository _kycRepo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        IKycReviewRepository kycRepo,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AdminService> logger)
    {
        _kycRepo = kycRepo;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<ApiResponse<List<KycReviewResponse>>> GetPendingKycAsync()
    {
        var reviews = await _kycRepo.GetPendingAsync();
        _logger.LogInformation("Found {Count} pending KYC reviews", reviews.Count);
        return ApiResponse<List<KycReviewResponse>>.Successfull("OK", reviews.Select(MapKycReview).ToList());
    }

    public async Task<ApiResponse<List<KycReviewResponse>>> GetAllKycAsync()
    {
        var reviews = await _kycRepo.GetAllAsync();
        return ApiResponse<List<KycReviewResponse>>.Successfull("OK", reviews.Select(MapKycReview).ToList());
    }

    public async Task<ApiResponse<string>> ApproveKycAsync(Guid userId, KycActionRequest req, Guid adminId)
    {
        var review = await _kycRepo.FindByUserIdAsync(userId);
        if (review == null) return ApiResponse<string>.Fail("KYC review not found.");
        if (review.Status == "Approved") return ApiResponse<string>.Fail("KYC already approved.");

        review.Status = "Approved";
        review.AdminNote = req.AdminNote;
        review.ReviewedBy = adminId;
        review.ReviewedAt = DateTime.Now;
        await _kycRepo.SaveChangesAsync();

        _logger.LogInformation("KYC approved for UserId: {UserId}", userId);

        var userActivated = await UpdateUserStatusAsync(userId, "Active");
        if (!userActivated)
        {
            _logger.LogError("Failed to activate user: {UserId}", userId);
            return ApiResponse<string>.Fail("KYC approved but failed to activate user. Contact support.");
        }

        _logger.LogInformation("User activated: {UserId}", userId);

        var walletCreated = await CreateWalletAsync(userId);
        if (!walletCreated)
            _logger.LogWarning("Failed to create wallet for user: {UserId}", userId);
        else
            _logger.LogInformation("Wallet created for user: {UserId}", userId);

        return ApiResponse<string>.Successfull("KYC approved. User account activated and wallet created.", "");
    }

    public async Task<ApiResponse<string>> RejectKycAsync(Guid userId, KycActionRequest req, Guid adminId)
    {
        var review = await _kycRepo.FindByUserIdAsync(userId);
        if (review == null) return ApiResponse<string>.Fail("KYC review not found.");
        if (review.Status == "Approved") return ApiResponse<string>.Fail("Cannot reject an already approved KYC.");

        review.Status = "Rejected";
        review.AdminNote = req.AdminNote;
        review.ReviewedBy = adminId;
        review.ReviewedAt = DateTime.UtcNow;
        await _kycRepo.SaveChangesAsync();

        _logger.LogInformation("KYC rejected for UserId: {UserId}", userId);
        await UpdateUserStatusAsync(userId, "Rejected");

        return ApiResponse<string>.Successfull("KYC rejected. User has been notified.", "");
    }

    public async Task<ApiResponse<string>> SyncKycAsync(SyncKycRequest req)
    {
        var existing = await _kycRepo.FindByUserIdAsync(req.UserId);

        if (existing != null)
        {
            existing.DocumentType = req.DocumentType;
            existing.DocumentNumber = req.DocumentNumber;
            existing.Status = "Pending";
            existing.AdminNote = null;
            existing.ReviewedBy = null;
            existing.ReviewedAt = null;
            existing.SubmittedAt = req.SubmittedAt;
        }
        else
        {
            await _kycRepo.AddAsync(new KycReview
            {
                Id = Guid.NewGuid(),
                UserId = req.UserId,
                UserFullName = req.UserFullName,
                UserEmail = req.UserEmail,
                DocumentType = req.DocumentType,
                DocumentNumber = req.DocumentNumber,
                Status = "Pending",
                SubmittedAt = req.SubmittedAt
            });
        }

        await _kycRepo.SaveChangesAsync();
        _logger.LogInformation("KYC synced for UserId: {UserId}", req.UserId);

        return ApiResponse<string>.Successfull("KYC synced.", "");
    }

    private async Task<bool> UpdateUserStatusAsync(Guid userId, string status)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
            client.DefaultRequestHeaders.Add("Authorization", authHeader);
            var response = await client.PutAsJsonAsync("/api/auth/update-status", new { UserId = userId, Status = status });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError("UpdateUserStatus failed: {Message}", ex.Message);
            return false;
        }
    }

    private async Task<bool> CreateWalletAsync(Guid userId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("WalletService");
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
            client.DefaultRequestHeaders.Add("Authorization", authHeader);
            var response = await client.PostAsJsonAsync("/api/wallet/create-internal", new { UserId = userId });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError("CreateWallet failed: {Message}", ex.Message);
            return false;
        }
    }

    private static KycReviewResponse MapKycReview(KycReview k) => new()
    {
        Id = k.Id,
        UserId = k.UserId,
        UserFullName = k.UserFullName,
        UserEmail = k.UserEmail,
        DocumentType = k.DocumentType,
        DocumentNumber = k.DocumentNumber,
        Status = k.Status,
        AdminNote = k.AdminNote,
        SubmittedAt = k.SubmittedAt,
        ReviewedAt = k.ReviewedAt
    };
}
