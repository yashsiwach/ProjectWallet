using Microsoft.EntityFrameworkCore;
using WalletService.Application.Interfaces;
using WalletService.Domain.Models;
using WalletService.Infrastructure.Data;

namespace WalletService.Infrastructure.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly WalletDbContext _db;

    public WalletRepository(WalletDbContext db) => _db = db;

    public Task<bool> ExistsForUserAsync(Guid userId) =>
        _db.Wallets.AnyAsync(w => w.UserId == userId);

    public Task<Wallet?> FindByUserIdAsync(Guid userId) =>
        _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);

    public async Task AddAsync(Wallet wallet) => await _db.Wallets.AddAsync(wallet);

    public async Task AddTransactionAsync(WalletTransaction transaction) =>
        await _db.WalletTransactions.AddAsync(transaction);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
