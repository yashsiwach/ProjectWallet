using AdminService.Domain.Models;

namespace AdminService.Application.Interfaces;

public interface IKycReviewRepository
{
    Task<List<KycReview>> GetPendingAsync();
    Task<List<KycReview>> GetAllAsync();
    Task<KycReview?> FindByUserIdAsync(Guid userId);
    Task AddAsync(KycReview review);
    Task SaveChangesAsync();
}
