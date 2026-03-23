namespace Reward_Service.DTOs;

//send by wallet after transfer
public class AwardPointsRequest
{
    public Guid UserId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty; 
}

//  user checks rewards
public class RewardResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int PointsBalance { get; set; }
    public int TotalEarned { get; set; }
    public string Tier { get; set; } = string.Empty;
    public string NextTier { get; set; } = string.Empty;
    public int PointsToNext { get; set; } 
    public DateTime CreatedAt { get; set; }
}
// One reward 
public class RewardTransactionResponse
{
    public Guid Id { get; set; }
    public int Points { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

//wrapper
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Successfull(string message, T data) =>
        new ApiResponse<T> { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message) =>
        new ApiResponse<T> { Success = false, Message = message, Data = default };
}