namespace NotificationService.DTOs;


public class NotificationResponse
{
    public string Id { get; set; } = string.Empty; 
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}


public class NotificationListResponse
{
    public List<NotificationResponse> Notifications { get; set; } = new();
    public long TotalCount { get; set; }
    public long UnreadCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}


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