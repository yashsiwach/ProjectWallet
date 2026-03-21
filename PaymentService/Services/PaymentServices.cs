using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.DTOs;
using PaymentService.Models;
using System.Net.Http.Headers;

namespace PaymentService.Services
{
    public class PaymentServices
    {
        private readonly PaymentDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PaymentServices(PaymentDbContext db, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _config = config;

        }
        //Topup wallet
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

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();
            //mock gateway
            var gatewaySuccess = MockGateway(req.Amount);
            if (!gatewaySuccess)
            {
                payment.Status = "Failed";
                payment.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                return ApiResponse<PaymentResponse>.Fail("Payment gateway rejected the transaction.");
            }
            //Wallet service call to credit wallet
            var credited = await CreditWalletAsync(userId, req.Amount, gatewayRef, req.Note);
            if (!credited)
            {
                payment.Status = "Failed";
                payment.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                return ApiResponse<PaymentResponse>.Fail("Payment approved but wallet credit failed. Contact support.");
            }

            payment.Status = "Success";
            payment.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return ApiResponse<PaymentResponse>.Successfull( $"Top-up successful. ₹{req.Amount} added to your wallet.",MapPayment(payment));
           
        }
        //History
        public async Task<ApiResponse<List<PaymentResponse>>> GetHistoryAsync(Guid userId)
        {
            var payments = await _db.Payments.Where(p => p.UserId == userId).OrderByDescending(p => p.CreatedAt).ToListAsync();
            var result = payments.Select(MapPayment).ToList();
            return ApiResponse<List<PaymentResponse>>.Successfull("OK", result);
        }
        //helpers
        private static bool MockGateway(decimal amount)
        {
            return true;
        }

        private async Task<bool> CreditWalletAsync(Guid userId, decimal amount, string reference, string? note)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("WalletService");
            
                var response = await client.PostAsJsonAsync("/api/wallet/credit",
                    new
                    {
                        UserId = userId,
                        Amount = amount,
                        Reference = reference,
                        Note = note ?? "Top-up via payment gateway"
                    });
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        private static string GenerateGatewayRef()
        {
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var randomPart = Guid.NewGuid().ToString("N")[..8].ToUpper();
            return $"PAY{datePart}{randomPart}";
        }
        private static PaymentResponse MapPayment(Payment p) => new PaymentResponse
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
}
