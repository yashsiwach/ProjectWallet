using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WalletService.DTOs;
using WalletService.Services;


namespace WalletService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly WalletServices _walletService;
        public WalletController(WalletServices walletService)
        {
            _walletService = walletService;
        }
        private Guid CurrentUserId =>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private string CurrentStatus =>User.FindFirstValue("Status") ?? "Pending";

        //POST /api/wallet/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateWallet()
        {
            if (CurrentStatus != "Active")
            {
                return BadRequest(ApiResponse<string>.Fail("Your account is not active. Please complete KYC first."));
            }
            var result = await _walletService.CreateWalletAsync(CurrentUserId);
            if (!result.Success)return BadRequest(result);
            return Ok(result);
        }

        //ET /api/wallet
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
            {
                return BadRequest(ApiResponse<string>.Fail("Your account is not active. Please complete KYC first."));
            }
            var result = await _walletService.CreditWalletAsync(req.UserId, req.Amount, req.Reference, req.Note);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        //POST /api/wallet/transfer
        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferRequest req)
        {
            if (CurrentStatus != "Active")
            {
                return BadRequest(ApiResponse<string>.Fail("Your account is not active. Please complete KYC first."));
            }
            var result = await _walletService.TransferAsync(CurrentUserId, req);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        //GET /api/wallet/history
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _walletService.GetHistoryAsync(CurrentUserId, page, pageSize);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
    public class CreditRequest
    {
        public Guid UserId { get; set; } 
        public decimal Amount { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}
