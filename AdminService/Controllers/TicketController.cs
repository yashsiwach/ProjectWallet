using AdminService.Data;
using AdminService.DTOs;
using AdminService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AdminService.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize] 
public class TicketController : ControllerBase
{
    private readonly AdminDbContext _db;
    private readonly ILogger<TicketController> _logger;

    public TicketController(AdminDbContext db, ILogger<TicketController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private Guid CurrentUserId =>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string CurrentUserEmail =>User.FindFirstValue(ClaimTypes.Email) ?? "";

    //  POST /api/tickets 

    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Subject) ||string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = "Subject and message are required."
            });

        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            UserId = CurrentUserId,
            UserEmail = CurrentUserEmail,
            Subject = req.Subject.Trim(),
            Message = req.Message.Trim(),
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };

        _db.SupportTickets.Add(ticket);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Ticket created by {Email}", CurrentUserEmail);

        return Ok(new ApiResponse<TicketResponse>
        {
            Success = true,
            Message = "Ticket submitted successfully.",
            Data = MapTicket(ticket)
        });
    }

    //  GET /api/tickets 
    [HttpGet]
    public async Task<IActionResult> GetMyTickets()
    {
        var tickets = await _db.SupportTickets
            .Where(t => t.UserId == CurrentUserId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var result = tickets.Select(MapTicket).ToList();

        return Ok(new ApiResponse<List<TicketResponse>>
        {
            Success = true,
            Message = "OK",
            Data = result
        });
    }

    private static TicketResponse MapTicket(SupportTicket t) =>
        new TicketResponse
        {
            Id = t.Id,
            UserId = t.UserId,
            UserEmail = t.UserEmail,
            Subject = t.Subject,
            Message = t.Message,
            Status = t.Status,
            AdminReply = t.AdminReply,
            CreatedAt = t.CreatedAt,
            RespondedAt = t.RespondedAt
        };
}

public class CreateTicketRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}