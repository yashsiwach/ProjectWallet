
using AuthService.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthServices _authService;
        public AuthController(AuthServices authService)
        {
            _authService = authService;
        }
        //id of userloged in 
        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        //register and return token
        // POST /api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            var result = await _authService.RegisterAsync(model);
            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(result);
        }

        //login and return token
        // POST /api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            var result = await _authService.LoginAsync(model);
            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(result);
        }

        // POST /api/auth/kyc
        [Authorize]
        [HttpPost("kyc")]
        public async Task<IActionResult> SubmitKyc([FromBody] KycRequest model)
        {
            var result = await _authService.SubmitKycAsync(CurrentUserId, model);
            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(result);
        }

        //GET /api/auth/profile
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var result = await _authService.GetProfileAsync(CurrentUserId);
            if (!result.Success)
                return BadRequest(result.Message);
            return Ok(result);
        }
        [HttpGet("user-by-email")]
        public async Task<IActionResult> GetUserByEmail([FromQuery] string email)
        {
            var user = await _authService.GetUserByEmailAsync(email);
            if (user == null) return NotFound();
            return Ok(new { UserId = user.Id });
        }
        [HttpPut("update-status")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest req)
        {
            var result = await _authService.UpdateUserStatusAsync(req.UserId, req.Status);
            if (!result) return NotFound();
            return Ok(new { message = "Status updated." });
        }
       
    }
    public class UpdateStatusRequest
    {
        public Guid UserId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}