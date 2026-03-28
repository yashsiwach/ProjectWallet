using WalletService.Domain.Models;

namespace WalletService.Application.Interfaces;

public interface IWalletRepository
{
    Task<bool> ExistsForUserAsync(Guid userId);
    Task<Wallet?> FindByUserIdAsync(Guid userId);
    Task AddAsync(Wallet wallet);
    Task AddTransactionAsync(WalletTransaction transaction);
    Task SaveChangesAsync();
}
