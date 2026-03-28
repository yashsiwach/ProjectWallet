using AdminService.DTOs;

namespace AdminService.Application.Interfaces;

public interface IAdminService
{
    Task<ApiResponse<List<KycReviewResponse>>> GetPendingKycAsync();
    Task<ApiResponse<List<KycReviewResponse>>> GetAllKycAsync();
    Task<ApiResponse<string>> ApproveKycAsync(Guid userId, KycActionRequest req, Guid adminId);
    Task<ApiResponse<string>> RejectKycAsync(Guid userId, KycActionRequest req, Guid adminId);
    Task<ApiResponse<string>> SyncKycAsync(SyncKycRequest req);
}
