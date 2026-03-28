namespace WalletService.DTOs;

public class TransferRequest
{
    public string ToEmail { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public class CreateInternalRequest
{
    public Guid UserId { get; set; }
}

// Moved from WalletController.cs — belongs in DTOs
public class CreditRequest
{
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string? Note { get; set; }
}

// Moved from WalletServices.cs — belongs in DTOs (internal service call response)
public class UserByEmailResponse
{
    public Guid UserId { get; set; }
}

public class WalletResponse
{
    public Guid Id { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TransactionResponse
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TransactionHistory
{
    public List<TransactionResponse> Transactions { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Successfull(T data, string message = "") =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Data = default, Message = message };
}
