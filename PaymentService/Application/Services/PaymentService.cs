using PaymentService.Application.Interfaces;
using PaymentService.Domain.Models;
using PaymentService.DTOs;
using System.Net.Http.Headers;

namespace PaymentService.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepo,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PaymentService> logger)
    {
        _paymentRepo = paymentRepo;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<ApiResponse<PaymentResponse>> TopUpAsync(Guid userId, Guid walletId, TopUpRequest req)
    {
        if (req.Amount <= 0)
            return ApiResponse<PaymentResponse>.Fail("Amount must be greater than zero.");
        if (req.Amount > 100000)
            return ApiResponse<PaymentResponse>.Fail("Maximum top-up amount is 1,00,000.");

        var gatewayRef = GenerateGatewayRef();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            WalletId = walletId,
            Amount = req.Amount,
            Type = "Topup",
            Status = "Pending",
            GatewayRef = gatewayRef,
            Note = req.Note,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        await _paymentRepo.AddAsync(payment);
        await _paymentRepo.SaveChangesAsync();

        var gatewaySuccess = MockGateway(req.Amount);
        if (!gatewaySuccess)
        {
            payment.Status = "Failed";
            payment.UpdatedAt = DateTime.Now;
            await _paymentRepo.SaveChangesAsync();
            return ApiResponse<PaymentResponse>.Fail("Payment gateway rejected the transaction.");
        }

        var credited = await CreditWalletAsync(userId, req.Amount, gatewayRef, req.Note);
        if (!credited)
        {
            payment.Status = "Failed";
            payment.UpdatedAt = DateTime.Now;
            await _paymentRepo.SaveChangesAsync();
            return ApiResponse<PaymentResponse>.Fail("Payment approved but wallet credit failed. Contact support.");
        }

        payment.Status = "Success";
        payment.UpdatedAt = DateTime.Now;
        await _paymentRepo.SaveChangesAsync();

        _logger.LogInformation("TopUp successful for UserId: {UserId}, Amount: {Amount}", userId, req.Amount);

        return ApiResponse<PaymentResponse>.Successfull($"Top-up successful. ₹{req.Amount} added to your wallet.", MapPayment(payment));
    }

    public async Task<ApiResponse<List<PaymentResponse>>> GetHistoryAsync(Guid userId)
    {
        var payments = await _paymentRepo.GetByUserIdAsync(userId);
        return ApiResponse<List<PaymentResponse>>.Successfull("OK", payments.Select(MapPayment).ToList());
    }

    private static bool MockGateway(decimal amount) => true;

    private async Task<bool> CreditWalletAsync(Guid userId, decimal amount, string reference, string? note)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("WalletService");
            var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authorization))
                client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authorization);

            var response = await client.PostAsJsonAsync("/api/wallet/credit",
                new { UserId = userId, Amount = amount, Reference = reference, Note = note ?? "Top-up via payment gateway" });

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError("CreditWallet failed: {Message}", ex.Message);
            return false;
        }
    }

    private static string GenerateGatewayRef()
    {
        var datePart = DateTime.Now.ToString("yyyyMMdd");
        var randomPart = Guid.NewGuid().ToString("N")[..8].ToUpper();
        return $"PAY{datePart}{randomPart}";
    }

    private static PaymentResponse MapPayment(Payment p) => new()
    {
        Id = p.Id,
        Amount = p.Amount,
        Type = p.Type,
        Status = p.Status,
        GatewayRef = p.GatewayRef,
        Note = p.Note,
        CreatedAt = p.CreatedAt
    };
}
