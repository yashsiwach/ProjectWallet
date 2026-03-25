using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.DTOs;
using NotificationService.Services;
using System.Collections;
using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography.Xml;
using static System.Net.Mime.MediaTypeNames;


namespace NotificationService.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly NotificationServices _notificationServices;

    public NotificationController(NotificationServices notificationServices)
    {
        _notificationServices = notificationServices;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET /api/notifications 

    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _notificationServices.GetNotificationsAsync(CurrentUserId, page, pageSize);
        return Ok(result);
    }

    // PUT /api/notifications/{id}/read 

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        var result = await _notificationServices.MarkAsReadAsync(id, CurrentUserId);
        return Ok(result);
    }

    //  PUT /api/notifications/read-all 

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var result = await _notificationServices.MarkAllAsReadAsync(CurrentUserId);
        return Ok(result);
    }
}
