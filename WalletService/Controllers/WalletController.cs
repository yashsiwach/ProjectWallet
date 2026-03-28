using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WalletService.Application.Interfaces;
using WalletService.DTOs;

namespace WalletService.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string CurrentStatus => User.FindFirstValue("Status") ?? "Pending";
    private string CurrentEmail => User.FindFirstValue("Email") ?? string.Empty;

    // POST /api/wallet/create
    [HttpPost("create")]
    public async Task<IActionResult> CreateWallet()
    {
        if (CurrentStatus != "Active")
            return BadRequest(ApiResponse<string>.Fail("Your account is not active. Please complete KYC first."));

        var result = await _walletService.CreateWalletAsync(CurrentUserId);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // GET /api/wallet
    [HttpGet]
    public async Task<IActionResult> GetWallet()
    {
        var result = await _walletService.GetWalletAsync(CurrentUserId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    // POST /api/wallet/credit
    [HttpPost("credit")]
    public async Task<IActionResult> CreditWallet([FromBody] CreditRequest req)
    {
        if (CurrentStatus != "Active")
            return BadRequest(ApiResponse<string>.Fail("Your account is not active. Please complete KYC first."));

        var result = await _walletService.CreditWalletAsync(req.UserId, req.Amount, req.Reference, req.Note);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // POST /api/wallet/transfer
    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest req)
    {
        if (CurrentStatus != "Active")
            return BadRequest(ApiResponse<string>.Fail("Your account is not active. Please complete KYC first."));

        var result = await _walletService.TransferAsync(CurrentUserId, CurrentEmail, req);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // GET /api/wallet/history
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _walletService.GetHistoryAsync(CurrentUserId, page, pageSize);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // POST /api/wallet/create-internal
    [HttpPost("create-internal")]
    public async Task<IActionResult> CreateWalletInternal([FromBody] CreateInternalRequest req)
    {
        var result = await _walletService.CreateWalletAsync(req.UserId);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
