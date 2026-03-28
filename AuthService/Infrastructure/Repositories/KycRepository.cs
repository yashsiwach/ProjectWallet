using AuthService.Application.Interfaces;
using AuthService.Domain.Models;
using AuthService.Infrastructure.Data;

namespace AuthService.Infrastructure.Repositories;

public class KycRepository : IKycRepository
{
    private readonly AuthDbContext _db;

    public KycRepository(AuthDbContext db) => _db = db;

    public async Task AddAsync(KycDocument kyc) => await _db.KycDocuments.AddAsync(kyc);

    public Task RemoveAsync(KycDocument kyc)
    {
        _db.KycDocuments.Remove(kyc);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
