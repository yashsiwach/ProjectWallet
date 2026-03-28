using NotificationService.Application.Interfaces;
using NotificationService.Domain.Models;
using NotificationService.DTOs;

namespace NotificationService.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repo;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(INotificationRepository repo, ILogger<NotificationService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task SaveNotificationAsync(string userId, string title, string message, string type)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            CreatedAt = DateTime.Now
        };

        await _repo.InsertAsync(notification);
        _logger.LogInformation("Notification saved for UserId: {UserId} Type: {Type}", userId, type);
    }

    public async Task<ApiResponse<NotificationListResponse>> GetNotificationsAsync(string userId, int page, int pageSize)
    {
        var notifications = await _repo.GetByUserIdAsync(userId, page, pageSize);
        var totalCount = await _repo.CountByUserIdAsync(userId);
        var unreadCount = await _repo.CountUnreadAsync(userId);

        var result = notifications.Select(n => new NotificationResponse
        {
            Id = n.Id ?? string.Empty,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        }).ToList();

        return ApiResponse<NotificationListResponse>.Successfull("OK", new NotificationListResponse
        {
            Notifications = result,
            TotalCount = totalCount,
            UnreadCount = unreadCount,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ApiResponse<string>> MarkAsReadAsync(string notificationId, string userId)
    {
        await _repo.MarkAsReadAsync(notificationId, userId);
        return ApiResponse<string>.Successfull("Marked as read.", "");
    }

    public async Task<ApiResponse<string>> MarkAllAsReadAsync(string userId)
    {
        await _repo.MarkAllAsReadAsync(userId);
        return ApiResponse<string>.Successfull("All notifications marked as read.", "");
    }
}
