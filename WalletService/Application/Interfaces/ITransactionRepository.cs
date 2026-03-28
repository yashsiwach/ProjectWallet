using WalletService.Domain.Models;

namespace WalletService.Application.Interfaces;

public interface ITransactionRepository
{
    Task<int> CountByWalletIdAsync(Guid walletId);
    Task<List<WalletTransaction>> GetByWalletIdPagedAsync(Guid walletId, int page, int pageSize);
}
