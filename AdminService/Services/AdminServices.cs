using AdminService.Data;
using AdminService.DTOs;
using AdminService.Models;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Services;

public class AdminServices
{
    private readonly AdminDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AdminServices> _logger;

    public AdminServices(AdminDbContext db, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ILogger<AdminServices> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }


    //  all KYC submissions waiting for admin review

    public async Task<ApiResponse<List<KycReviewResponse>>> GetPendingKycAsync()
    {
        var reviews = await _db.KycReviews.Where(k => k.Status == "Pending").OrderBy(k => k.SubmittedAt).ToListAsync();

        _logger.LogInformation("Found {Count} pending KYC reviews", reviews.Count);

        var result = reviews.Select(MapKycReview).ToList();
        return ApiResponse<List<KycReviewResponse>>.Successfull("OK", result);
    }

    public async Task<ApiResponse<List<KycReviewResponse>>> GetAllKycAsync()
    {
        var reviews = await _db.KycReviews.OrderByDescending(k => k.SubmittedAt).ToListAsync();

        var result = reviews.Select(MapKycReview).ToList();
        return ApiResponse<List<KycReviewResponse>>.Successfull("OK", result);
    }

    // APPROVE KYC
    // 1. Update KycReview status in AdminDB
    // 2. Call AuthService to set user Status = Active
    // 3. Call WalletService to create wallet automatically
    public async Task<ApiResponse<string>> ApproveKycAsync(Guid userId, KycActionRequest req, Guid adminId)
    {
        //Find KYC review
        var review = await _db.KycReviews.FirstOrDefaultAsync(k => k.UserId == userId);

        if (review == null)
            return ApiResponse<string>.Fail("KYC review not found.");

        if (review.Status == "Approved")
            return ApiResponse<string>.Fail("KYC already approved.");

        //Update
        review.Status = "Approved";
        review.AdminNote = req.AdminNote;
        review.ReviewedBy = adminId;
        review.ReviewedAt = DateTime.Now;
        await _db.SaveChangesAsync();

        _logger.LogInformation("KYC approved for UserId: {UserId}", userId);

        // activate user
        var userActivated = await UpdateUserStatusAsync(userId, "Active");

        if (!userActivated)
        {
            _logger.LogError("Failed to activate user: {UserId}", userId);
            return ApiResponse<string>.Fail("KYC approved but failed to activate user. Contact support.");
        }

        _logger.LogInformation("User activated: {UserId}", userId);

        //  create wallet automatically
        var walletCreated = await CreateWalletAsync(userId);
        if (!walletCreated)
        {
            _logger.LogWarning("Failed to create wallet for user: {UserId}", userId);
        }

        _logger.LogInformation("Wallet created for user: {UserId}", userId);

        return ApiResponse<string>.Successfull("KYC approved. User account activated and wallet created.", "");
    }


    public async Task<ApiResponse<string>> RejectKycAsync(Guid userId, KycActionRequest req, Guid adminId)
    {
        var review = await _db.KycReviews.FirstOrDefaultAsync(k => k.UserId == userId);

        if (review == null)
            return ApiResponse<string>.Fail("KYC review not found.");

        if (review.Status == "Approved")
            return ApiResponse<string>.Fail("Cannot reject an already approved KYC.");

        review.Status = "Rejected";
        review.AdminNote = req.AdminNote;
        review.ReviewedBy = adminId;
        review.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("KYC rejected for UserId: {UserId}", userId);

        await UpdateUserStatusAsync(userId, "Rejected");

        return ApiResponse<string>.Successfull("KYC rejected. User has been notified.", "");
    }


    public async Task<ApiResponse<List<TicketResponse>>> GetTicketsAsync(string? status = null)
    {
        var query = _db.SupportTickets.AsQueryable();

        // GET /api/admin/tickets?status=Open
        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);

        var tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

        var result = tickets.Select(MapTicket).ToList();
        return ApiResponse<List<TicketResponse>>.Successfull("OK", result);
    }


    public async Task<ApiResponse<string>> ReplyToTicketAsync( Guid ticketId, TicketReplyRequest req, Guid adminId)
    {
        var ticket = await _db.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
            return ApiResponse<string>.Fail("Ticket not found.");

        if (ticket.Status == "Closed")
            return ApiResponse<string>.Fail("Ticket is already closed.");

        ticket.AdminReply = req.Reply;
        ticket.RespondedBy = adminId;
        ticket.RespondedAt = DateTime.Now;
        ticket.Status = "Responded";
        await _db.SaveChangesAsync();

        _logger.LogInformation("Ticket {TicketId} replied by admin {AdminId}",
            ticketId, adminId);

        return ApiResponse<string>.Successfull("Reply sent successfully.", "");
    }


    // Calls AuthService to update user status
    private async Task<bool> UpdateUserStatusAsync(Guid userId, string status)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
            client.DefaultRequestHeaders.Add("Authorization", authHeader);

            var response = await client.PutAsJsonAsync($"/api/auth/update-status", new { UserId = userId, Status = status });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError("UpdateUserStatus failed: {Message}", ex.Message);
            return false;
        }
    }
    public async Task<ApiResponse<string>> SyncKycAsync(SyncKycRequest req)
    {
        // Check if KYC already exists for this user
        var existing = await _db.KycReviews.FirstOrDefaultAsync(k => k.UserId == req.UserId);

        if (existing != null)
        {
            //  resubmitted KYC update 
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
           
            var review = new KycReview
            {
                Id = Guid.NewGuid(),
                UserId = req.UserId,
                UserFullName = req.UserFullName,
                UserEmail = req.UserEmail,
                DocumentType = req.DocumentType,
                DocumentNumber = req.DocumentNumber,
                Status = "Pending",
                SubmittedAt = req.SubmittedAt
            };
            _db.KycReviews.Add(review);
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("KYC synced for UserId: {UserId}", req.UserId);

        return ApiResponse<string>.Successfull("KYC synced.", "");
    }

    // Calls WalletService to create wallet for user
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


    private static KycReviewResponse MapKycReview(KycReview k) =>
        new KycReviewResponse
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


    private static TicketResponse MapTicket(SupportTicket t) =>
       new TicketResponse
       {
           Id = t.Id,
           UserId = t.UserId,
           UserEmail = t.UserEmail,
           Subject = t.Subject,
           Message = t.Message,
           Status = t.Status,
           AdminReply = t.AdminReply,
           CreatedAt = t.CreatedAt,
           RespondedAt = t.RespondedAt
       };
}