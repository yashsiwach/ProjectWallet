using NotificationService.DTOs;

namespace NotificationService.Application.Interfaces;

public interface INotificationService
{
    Task SaveNotificationAsync(string userId, string title, string message, string type);
    Task<ApiResponse<NotificationListResponse>> GetNotificationsAsync(string userId, int page, int pageSize);
    Task<ApiResponse<string>> MarkAsReadAsync(string notificationId, string userId);
    Task<ApiResponse<string>> MarkAllAsReadAsync(string userId);
}
