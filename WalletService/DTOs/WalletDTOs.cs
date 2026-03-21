namespace WalletService.DTOs;

//user want to send to other
public class TransferRequest
{
    public String ToEmail { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string?Note { get; set; }
}

//wallet details
public class WalletResponse
{
    public Guid Id { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public DateTime CreatedAt { get; set; }
}

// transaction details
public class TransactionResponse
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;//credit or debit
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? ToEmail { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TransactionHistory
{
    public List<TransactionResponse> Transactions { get; set; } = new List<TransactionResponse>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public static ApiResponse<T> Successfull(T data, string message = "")
    {
        return new ApiResponse<T> { Success = true, Data = data, Message = message };
    }
    public static ApiResponse<T> Fail(string message)
    {
        return new ApiResponse<T> { Success = false, Data=default, Message = message  };
    }
}
