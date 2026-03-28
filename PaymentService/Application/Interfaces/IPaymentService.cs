using PaymentService.DTOs;

namespace PaymentService.Application.Interfaces;

public interface IPaymentService
{
    Task<ApiResponse<PaymentResponse>> TopUpAsync(Guid userId, Guid walletId, TopUpRequest req);
    Task<ApiResponse<List<PaymentResponse>>> GetHistoryAsync(Guid userId);
}
