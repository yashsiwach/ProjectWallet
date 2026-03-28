using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Interfaces;
using System.Security.Claims;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET /api/notifications
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _notificationService.GetNotificationsAsync(CurrentUserId, page, pageSize);
        return Ok(result);
    }

    // PUT /api/notifications/{id}/read
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        var result = await _notificationService.MarkAsReadAsync(id, CurrentUserId);
        return Ok(result);
    }

    // PUT /api/notifications/read-all
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var result = await _notificationService.MarkAllAsReadAsync(CurrentUserId);
        return Ok(result);
    }
}
