using Microsoft.EntityFrameworkCore;
using WalletService.Application.Interfaces;
using WalletService.Domain.Models;
using WalletService.Infrastructure.Data;

namespace WalletService.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly WalletDbContext _db;

    public TransactionRepository(WalletDbContext db) => _db = db;

    public Task<int> CountByWalletIdAsync(Guid walletId) =>
        _db.WalletTransactions.Where(t => t.WalletId == walletId).CountAsync();

    public Task<List<WalletTransaction>> GetByWalletIdPagedAsync(Guid walletId, int page, int pageSize) =>
        _db.WalletTransactions
            .Where(t => t.WalletId == walletId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
}
