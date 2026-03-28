using NotificationService.Domain.Models;

namespace NotificationService.Application.Interfaces;

public interface INotificationRepository
{
    Task InsertAsync(Notification notification);
    Task<List<Notification>> GetByUserIdAsync(string userId, int page, int pageSize);
    Task<long> CountByUserIdAsync(string userId);
    Task<long> CountUnreadAsync(string userId);
    Task MarkAsReadAsync(string id, string userId);
    Task MarkAllAsReadAsync(string userId);
}
