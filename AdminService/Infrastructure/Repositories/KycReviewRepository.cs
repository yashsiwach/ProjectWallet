using AdminService.Application.Interfaces;
using AdminService.Domain.Models;
using AdminService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Infrastructure.Repositories;

public class KycReviewRepository : IKycReviewRepository
{
    private readonly AdminDbContext _db;

    public KycReviewRepository(AdminDbContext db) => _db = db;

    public Task<List<KycReview>> GetPendingAsync() =>
        _db.KycReviews.Where(k => k.Status == "Pending").OrderBy(k => k.SubmittedAt).ToListAsync();

    public Task<List<KycReview>> GetAllAsync() =>
        _db.KycReviews.OrderByDescending(k => k.SubmittedAt).ToListAsync();

    public Task<KycReview?> FindByUserIdAsync(Guid userId) =>
        _db.KycReviews.FirstOrDefaultAsync(k => k.UserId == userId);

    public async Task AddAsync(KycReview review) => await _db.KycReviews.AddAsync(review);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
