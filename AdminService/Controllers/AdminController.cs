using AdminService.DTOs;
using AdminService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AdminService.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")] 
public class AdminController : ControllerBase
{
    private readonly AdminServices _adminServices;

    public AdminController(AdminServices adminServices)
    {
        _adminServices = adminServices;
    }

    private Guid CurrentAdminId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET /api/admin/kyc/pending 
    [HttpGet("kyc/pending")]
    public async Task<IActionResult> GetPendingKyc()
    {
        var result = await _adminServices.GetPendingKycAsync();
        return Ok(result);
    }

    //  GET /api/admin/kyc/all
    [HttpGet("kyc/all")]
    public async Task<IActionResult> GetAllKyc()
    {
        var result = await _adminServices.GetAllKycAsync();
        return Ok(result);
    }

    //PUT /api/admin/kyc/{userId}/approve
    // Approves KYC → activates user → creates wallet
    [HttpPut("kyc/{userId:guid}/approve")]
    public async Task<IActionResult> ApproveKyc(Guid userId, [FromBody] KycActionRequest req)
    {
        var result = await _adminServices.ApproveKycAsync(userId, req, CurrentAdminId);

        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    //  PUT /api/admin/kyc/{userId}/reject
    [HttpPut("kyc/{userId:guid}/reject")]
    public async Task<IActionResult> RejectKyc( Guid userId, [FromBody] KycActionRequest req)
    {
        var result = await _adminServices.RejectKycAsync(
            userId, req, CurrentAdminId);

        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // ── GET /api/admin/tickets ────────────────────────────────────────────────
    // Optional filter: /api/admin/tickets?status=Open
    [HttpGet("tickets")]
    public async Task<IActionResult> GetTickets([FromQuery] string? status = null)
    {
        var result = await _adminServices.GetTicketsAsync(status);
        return Ok(result);
    }
    [AllowAnonymous]
    [HttpPost("kyc/sync")]
    public async Task<IActionResult> SyncKyc([FromBody] SyncKycRequest req)
    {
        var result = await _adminServices.SyncKycAsync(req);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // ── PUT /api/admin/tickets/{ticketId}/reply ───────────────────────────────
    [HttpPut("tickets/{ticketId:guid}/reply")]
    public async Task<IActionResult> ReplyToTicket(Guid ticketId, [FromBody] TicketReplyRequest req)
    {
        var result = await _adminServices.ReplyToTicketAsync(ticketId, req, CurrentAdminId);

        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}