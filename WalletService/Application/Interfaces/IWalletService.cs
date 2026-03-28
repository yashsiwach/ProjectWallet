using WalletService.DTOs;

namespace WalletService.Application.Interfaces;

public interface IWalletService
{
    Task<ApiResponse<WalletResponse>> CreateWalletAsync(Guid userId);
    Task<ApiResponse<WalletResponse>> GetWalletAsync(Guid userId);
    Task<ApiResponse<WalletResponse>> CreditWalletAsync(Guid userId, decimal amount, string reference, string? note);
    Task<ApiResponse<string>> TransferAsync(Guid senderUserId, string senderEmail, TransferRequest req);
    Task<ApiResponse<TransactionHistory>> GetHistoryAsync(Guid userId, int page, int pageSize);
}
