using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.DTOs;
using PaymentService.Services;
using System.Security.Claims;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/payment")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly PaymentServices _paymentServices;

    public PaymentController(PaymentServices paymentServices)
    {
        _paymentServices = paymentServices;
    }

    private Guid CurrentUserId =>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string CurrentStatus =>User.FindFirstValue("status") ?? "Pending";

    //  POST /api/payment/topup
    [HttpPost("topup")]
    public async Task<IActionResult> TopUp([FromBody] TopUpRequest req)
    {
        if (CurrentStatus != "Active")
            return BadRequest(ApiResponse<string>.Fail("Your account is not active. Please complete KYC first."));

        var result = await _paymentServices.TopUpAsync(CurrentUserId, req.WalletId, req);

        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // GET /api/payment/history 
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var result = await _paymentServices.GetHistoryAsync(CurrentUserId);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}