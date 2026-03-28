using WalletService.Application.Interfaces;
using WalletService.Domain.Models;
using WalletService.DTOs;

namespace WalletService.Application.Services;

public class WalletService : IWalletService
{
    private readonly IWalletRepository _walletRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRabbitMqPublisher _rabbitMq;
    private readonly IEmailService _emailService;
    private readonly ILogger<WalletService> _logger;

    public WalletService(
        IWalletRepository walletRepo,
        ITransactionRepository transactionRepo,
        IHttpClientFactory httpClientFactory,
        IRabbitMqPublisher rabbitMq,
        IEmailService emailService,
        ILogger<WalletService> logger)
    {
        _walletRepo = walletRepo;
        _transactionRepo = transactionRepo;
        _httpClientFactory = httpClientFactory;
        _rabbitMq = rabbitMq;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ApiResponse<WalletResponse>> CreateWalletAsync(Guid userId)
    {
        var exists = await _walletRepo.ExistsForUserAsync(userId);
        if (exists)
            return ApiResponse<WalletResponse>.Fail("Wallet already exists for this user.");

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

        await _walletRepo.AddAsync(wallet);
        await _walletRepo.SaveChangesAsync();

        _logger.LogInformation("Wallet created for UserId: {UserId}", userId);

        return ApiResponse<WalletResponse>.Successfull(MapWallet(wallet), "Wallet Created.");
    }

    public async Task<ApiResponse<WalletResponse>> GetWalletAsync(Guid userId)
    {
        var wallet = await _walletRepo.FindByUserIdAsync(userId);
        if (wallet == null)
            return ApiResponse<WalletResponse>.Fail("Wallet not found for this user.");

        return ApiResponse<WalletResponse>.Successfull(MapWallet(wallet), "OK");
    }

    public async Task<ApiResponse<WalletResponse>> CreditWalletAsync(Guid userId, decimal amount, string reference, string? note)
    {
        var wallet = await _walletRepo.FindByUserIdAsync(userId);
        if (wallet == null) return ApiResponse<WalletResponse>.Fail("Wallet not found.");
        if (wallet.IsLocked) return ApiResponse<WalletResponse>.Fail("Wallet is locked. Contact support.");

        wallet.Balance += amount;
        wallet.UpdatedAt = DateTime.Now;

        await _walletRepo.AddTransactionAsync(new WalletTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            ToWalletId = null,
            Type = "topup",
            Amount = amount,
            BalanceAfter = wallet.Balance,
            Status = "Success",
            Reference = reference,
            Note = note,
            CreatedAt = DateTime.Now
        });

        await _walletRepo.SaveChangesAsync();

        _logger.LogInformation("Wallet credited for UserId: {UserId}, Amount: {Amount}", userId, amount);

        return ApiResponse<WalletResponse>.Successfull(MapWallet(wallet), "Wallet credited successfully.");
    }

    public async Task<ApiResponse<string>> TransferAsync(Guid senderUserId, string senderEmail, TransferRequest req)
    {
        if (req.Amount <= 0)
            return ApiResponse<string>.Fail("Amount must be greater than zero.");
        if (req.Amount > 100000)
            return ApiResponse<string>.Fail("Maximum transfer amount is 1,00,000.");

        var senderWallet = await _walletRepo.FindByUserIdAsync(senderUserId);
        if (senderWallet == null) return ApiResponse<string>.Fail("Your wallet not found.");
        if (senderWallet.IsLocked) return ApiResponse<string>.Fail("Your wallet is locked. Contact support.");
        if (senderWallet.Balance < req.Amount) return ApiResponse<string>.Fail("Insufficient balance.");

        var receiverUserId = await GetUserIdByEmailAsync(req.ToEmail);
        if (receiverUserId == null) return ApiResponse<string>.Fail("Receiver not found.");
        if (receiverUserId == senderUserId) return ApiResponse<string>.Fail("Cannot transfer to yourself.");

        var receiverWallet = await _walletRepo.FindByUserIdAsync(receiverUserId.Value);
        if (receiverWallet == null) return ApiResponse<string>.Fail("Receiver wallet not found.");
        if (receiverWallet.IsLocked) return ApiResponse<string>.Fail("Receiver wallet is locked.");

        var reference = GenerateReference();

        senderWallet.Balance -= req.Amount;
        senderWallet.UpdatedAt = DateTime.Now;

        await _walletRepo.AddTransactionAsync(new WalletTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = senderWallet.Id,
            ToWalletId = receiverWallet.Id,
            Type = "Debit",
            Amount = req.Amount,
            BalanceAfter = senderWallet.Balance,
            Status = "Success",
            Reference = reference + "_OUT",
            Note = req.Note,
            CreatedAt = DateTime.Now
        });

        receiverWallet.Balance += req.Amount;
        receiverWallet.UpdatedAt = DateTime.Now;

        await _walletRepo.AddTransactionAsync(new WalletTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = receiverWallet.Id,
            ToWalletId = senderWallet.Id,
            Type = "Credit",
            Amount = req.Amount,
            BalanceAfter = receiverWallet.Balance,
            Status = "Success",
            Reference = reference + "_IN",
            Note = req.Note,
            CreatedAt = DateTime.Now
        });

        await _walletRepo.SaveChangesAsync();

        _logger.LogInformation("Transfer completed. Reference: {Reference}", reference);

        _rabbitMq.Publish("transfer_completed", new
        {
            SenderUserId = senderUserId,
            ReceiverUserId = receiverUserId,
            Amount = req.Amount,
            Reference = reference,
            Reason = "transfer_completed"
        });

        _rabbitMq.Publish("notifications", new
        {
            UserId = senderUserId.ToString(),
            Title = "Transfer Sent",
            Message = $"You sent ₹{req.Amount} successfully. Ref: {reference}",
            Type = "transfer_completed"
        });

        _rabbitMq.Publish("notifications", new
        {
            UserId = receiverUserId.ToString(),
            Title = "Money Received",
            Message = $"You received ₹{req.Amount}. Ref: {reference}",
            Type = "transfer_completed"
        });

        _ = _emailService.SendTransferConfirmationAsync(
            senderEmail,
            "Transfer Confirmation",
            $"Hi,\n\nYou sent ₹{req.Amount} to {req.ToEmail}.\nReference: {reference}\n\nThank you for using ProjectWallet."
        );

        _ = _emailService.SendTransferConfirmationAsync(
            req.ToEmail,
            "Money Received",
            $"Hi,\n\nYou received ₹{req.Amount} from {senderEmail}.\nReference: {reference}\n\nThank you for using ProjectWallet."
        );

        return ApiResponse<string>.Successfull(reference, $"Transfer successful. Reference: {reference}");
    }

    public async Task<ApiResponse<TransactionHistory>> GetHistoryAsync(Guid userId, int page, int pageSize)
    {
        var wallet = await _walletRepo.FindByUserIdAsync(userId);
        if (wallet == null) return ApiResponse<TransactionHistory>.Fail("Wallet not found.");

        var totalCount = await _transactionRepo.CountByWalletIdAsync(wallet.Id);
        var transactions = await _transactionRepo.GetByWalletIdPagedAsync(wallet.Id, page, pageSize);

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

    private async Task<Guid?> GetUserIdByEmailAsync(string email)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");
            var response = await client.GetAsync($"/api/auth/user-by-email?email={Uri.EscapeDataString(email)}");
            if (!response.IsSuccessStatusCode) return null;
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

    private static WalletResponse MapWallet(Wallet w) => new()
    {
        Id = w.Id,
        Balance = w.Balance,
        Currency = w.Currency,
        IsLocked = w.IsLocked,
        CreatedAt = w.CreatedAt
    };
}
