using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reward_Service.Application.Interfaces;
using Reward_Service.DTOs;
using System.Security.Claims;

namespace Reward_Service.Controllers;

[ApiController]
[Route("api/reward")]
[Authorize]
public class RewardController : ControllerBase
{
    private readonly IRewardService _rewardService;

    public RewardController(IRewardService rewardService)
    {
        _rewardService = rewardService;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET /api/reward
    [HttpGet]
    public async Task<IActionResult> GetRewards()
    {
        var result = await _rewardService.GetRewardsAsync(CurrentUserId);
        return Ok(result);
    }

    // GET /api/reward/history
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var result = await _rewardService.GetHistoryAsync(CurrentUserId);
        return Ok(result);
    }

    // POST /api/reward/award
    [AllowAnonymous]
    [HttpPost("award")]
    public async Task<IActionResult> AwardPoints([FromBody] AwardPointsRequest req)
    {
        var result = await _rewardService.AwardPointsAsync(req);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
