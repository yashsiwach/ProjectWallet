using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reward_Service.DTOs;
using Reward_Service.Services;
using System.Security.Claims;

namespace Reward_Service.Controllers;

[ApiController]
[Route("api/reward")]
[Authorize]
public class RewardController : ControllerBase
{
    private readonly RewardServices _rewardServices;

    public RewardController(RewardServices rewardServices)
    {
        _rewardServices = rewardServices;
    }

    private Guid CurrentUserId =>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    //GET /api/reward 
    [HttpGet]
    public async Task<IActionResult> GetRewards()
    {
        var result = await _rewardServices.GetRewardsAsync(CurrentUserId);
        return Ok(result);
    }

    //GET /api/reward/history
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var result = await _rewardServices.GetHistoryAsync(CurrentUserId);
        return Ok(result);
    }

    //POST /api/reward/award
    [AllowAnonymous] 
    [HttpPost("award")]
    public async Task<IActionResult> AwardPoints([FromBody] AwardPointsRequest req)
    {
        var result = await _rewardServices.AwardPointsAsync(req);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}