using Microsoft.EntityFrameworkCore;
using WalletService.Data;
using WalletService.DTOs;
using WalletService.Models;

namespace WalletService.Services
{
    public class WalletServices
    {
        private readonly WalletDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        public WalletServices(WalletDbContext db, IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
        }
        //make wallet
        public async Task<ApiResponse<WalletResponse>> CreateWalletAsync(Guid userId)
        {
            var exists = await _db.Wallets.AnyAsync(w => w.UserId == userId);
            if (exists)
            {
                return ApiResponse<WalletResponse>.Fail("Wallet already exists for this user.");

            }
            var wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Balance = 0,
                Currency = "INR",
                IsLocked = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now

            };
            _db.Wallets.Add(wallet);
            await _db.SaveChangesAsync();
            return ApiResponse<WalletResponse>.Successfull(MapWallet(wallet), "Wallet Created.");
        }

        // Returns wallet balance and details
        public async Task<ApiResponse<WalletResponse>> GetWalletAsync(Guid userId)
        {
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
            {
                return ApiResponse<WalletResponse>.Fail("Wallet not found for this user.");
            }
            return ApiResponse<WalletResponse>.Successfull(MapWallet(wallet), "OK");
        }
        //Adds money 
        public async Task<ApiResponse<WalletResponse>> CreditWalletAsync(Guid userId, decimal amount, string reference, string? note)
        {
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
                return ApiResponse<WalletResponse>.Fail("Wallet not found.");

            if (wallet.IsLocked)
                return ApiResponse<WalletResponse>.Fail("Wallet is locked. Contact support.");

            wallet.Balance += amount;
            wallet.UpdatedAt = DateTime.Now;

            var transaction = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                ToWalletId = null,        // null because this is a top-up
                Type = "topup",
                Amount = amount,
                BalanceAfter = wallet.Balance,
                Status = "Success",
                Reference = reference,
                Note = note,
                CreatedAt = DateTime.Now
            };
            _db.WalletTransactions.Add(transaction);
            await _db.SaveChangesAsync();
            return ApiResponse<WalletResponse>.Successfull(MapWallet(wallet), "Wallet credited successfully.");
        }

        //transfer money
        public async Task<ApiResponse<string>> TransferAsync(Guid senderUserId, TransferRequest req)
        {
            //validation
            if (req.Amount <= 0)
                return ApiResponse<string>.Fail("Amount must be greater than zero.");
            if (req.Amount > 100000)
                return ApiResponse<string>.Fail("Maximum transfer amount is 1,00,000.");

            //Load sender wallet
            var senderWallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == senderUserId);
            if (senderWallet == null)
                return ApiResponse<string>.Fail("Your wallet not found.");

            //validation
            if (senderWallet.IsLocked)
                return ApiResponse<string>.Fail("Your wallet is locked. Contact support.");
            if (senderWallet.Balance < req.Amount)
                return ApiResponse<string>.Fail("Insufficient balance.");

            //get receiver wallet by email
            var receiverUserId = await GetUserIdByEmailAsync(req.ToEmail);
            if (receiverUserId == null)
                return ApiResponse<string>.Fail("Receiver not found.");
            //why transfering to yourself
            if (receiverUserId == senderUserId)
                return ApiResponse<string>.Fail("Cannot transfer to yourself.");

            var receiverWallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == receiverUserId);

            //validation
            if (receiverWallet == null)
                return ApiResponse<string>.Fail("Receiver wallet not found.");
            if (receiverWallet.IsLocked)
                return ApiResponse<string>.Fail("Receiver wallet is locked.");
            var reference = GenerateReference();

            senderWallet.Balance -= req.Amount;
            senderWallet.UpdatedAt = DateTime.Now;

            //record transaction
            _db.WalletTransactions.Add(new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = senderWallet.Id,
                ToWalletId = receiverWallet.Id,
                Type = "Debit",
                Amount = req.Amount,
                BalanceAfter = senderWallet.Balance,
                Status = "Success",
                Reference = reference+"_OUT",
                Note = req.Note,
                CreatedAt = DateTime.Now
            });

            receiverWallet.Balance += req.Amount;
            receiverWallet.UpdatedAt = DateTime.Now;

            // Receiver transaction record 
            _db.WalletTransactions.Add(new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = receiverWallet.Id,
                ToWalletId = senderWallet.Id,
                Type = "Credit",
                Amount = req.Amount,
                BalanceAfter = receiverWallet.Balance,
                Status = "Success",
                Reference = reference+"_IN",
                Note = req.Note,
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
            return ApiResponse<string>.Successfull(reference, $"Transfer successful. Reference: {reference}");
        }

        // TRANSACTION HISTORY

        public async Task<ApiResponse<TransactionHistory>> GetHistoryAsync(Guid userId, int page, int pageSize)
        {
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null) return ApiResponse<TransactionHistory>.Fail("Wallet not found.");

            var totalCount = await _db.WalletTransactions.Where(t => t.WalletId == wallet.Id).CountAsync();
            var transactions = await _db.WalletTransactions.Where(t => t.WalletId == wallet.Id).OrderByDescending(t => t.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var result = transactions.Select(t => new TransactionResponse
            {
                Id = t.Id,
                Type = t.Type,
                Amount = t.Amount,
                BalanceAfter = t.BalanceAfter,
                Status = t.Status,
                Reference = t.Reference,
                Note = t.Note,
                CreatedAt = t.CreatedAt
            }).ToList();

            return ApiResponse<TransactionHistory>.Successfull(new TransactionHistory
            {
                Transactions = result,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            }, "OK");
        }
        //Helper
        private async Task<Guid?> GetUserIdByEmailAsync(string email)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("AuthService");
                var response = await client.GetAsync($"/api/auth/user-by-email?email={Uri.EscapeDataString(email)}");
                if (!response.IsSuccessStatusCode)return null;
                var result = await response.Content.ReadFromJsonAsync<UserByEmailResponse>();
                return result?.UserId;
            }
            catch
            {
                return null;
            }
        }
        private static string GenerateReference()
        {
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var randomPart = Guid.NewGuid().ToString("N")[..6].ToUpper();
            return $"TF{datePart}{randomPart}";
        }
        private static WalletResponse MapWallet(Wallet w) => new WalletResponse
        {
            Id = w.Id,
            Balance = w.Balance,
            Currency = w.Currency,
            IsLocked = w.IsLocked,
            CreatedAt = w.CreatedAt
        };
    }
}
internal class UserByEmailResponse
{
    public Guid UserId { get; set; }
}