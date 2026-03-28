using AdminService.Application.Interfaces;
using AdminService.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AdminService.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
public class TicketController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string CurrentUserEmail => User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    // POST /api/tickets
    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Subject) || string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(ApiResponse<string>.Fail("Subject and message are required."));

        var result = await _ticketService.CreateTicketAsync(CurrentUserId, CurrentUserEmail, req);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // GET /api/tickets
    [HttpGet]
    public async Task<IActionResult> GetMyTickets()
    {
        var result = await _ticketService.GetMyTicketsAsync(CurrentUserId);
        return Ok(result);
    }
}
