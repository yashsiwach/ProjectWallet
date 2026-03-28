using Reward_Service.Application.Interfaces;
using Reward_Service.Domain.Models;
using Reward_Service.DTOs;

namespace Reward_Service.Application.Services;

public class RewardService : IRewardService
{
    private readonly IRewardRepository _rewardRepo;
    private readonly ILogger<RewardService> _logger;

    private const int PointsPerTransfer = 10;
    private const int SilverThreshold = 1000;
    private const int GoldThreshold = 5000;

    public RewardService(IRewardRepository rewardRepo, ILogger<RewardService> logger)
    {
        _rewardRepo = rewardRepo;
        _logger = logger;
    }

    public async Task<ApiResponse<RewardResponse>> AwardPointsAsync(AwardPointsRequest req)
    {
        var alreadyRewarded = await _rewardRepo.IsAlreadyRewardedAsync(req.UserId, req.Reference);
        if (alreadyRewarded)
            return ApiResponse<RewardResponse>.Fail("Points already awarded for this transaction.");

        var reward = await _rewardRepo.FindByUserIdAsync(req.UserId);
        if (reward == null)
        {
            reward = new Reward
            {
                Id = Guid.NewGuid(),
                UserId = req.UserId,
                PointsBalance = 0,
                TotalEarned = 0,
                Tier = "Bronze",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            await _rewardRepo.AddAsync(reward);
        }

        reward.PointsBalance += PointsPerTransfer;
        reward.TotalEarned += PointsPerTransfer;
        reward.UpdatedAt = DateTime.Now;
        reward.Tier = CalculateTier(reward.PointsBalance);

        await _rewardRepo.AddTransactionAsync(new RewardTransaction
        {
            Id = Guid.NewGuid(),
            UserId = req.UserId,
            Points = PointsPerTransfer,
            Reason = req.Reason,
            Reference = req.Reference,
            CreatedAt = DateTime.UtcNow
        });

        await _rewardRepo.SaveChangesAsync();

        _logger.LogInformation("Awarded {Points} points to UserId: {UserId}", PointsPerTransfer, req.UserId);

        return ApiResponse<RewardResponse>.Successfull($"Awarded {PointsPerTransfer} points.", MapReward(reward));
    }

    public async Task<ApiResponse<RewardResponse>> GetRewardsAsync(Guid userId)
    {
        var reward = await _rewardRepo.FindByUserIdAsync(userId);
        if (reward == null)
        {
            return ApiResponse<RewardResponse>.Successfull("OK", new RewardResponse
            {
                UserId = userId,
                PointsBalance = 0,
                TotalEarned = 0,
                Tier = "Bronze",
                NextTier = "Silver",
                PointsToNext = SilverThreshold
            });
        }

        return ApiResponse<RewardResponse>.Successfull("OK", MapReward(reward));
    }

    public async Task<ApiResponse<List<RewardTransactionResponse>>> GetHistoryAsync(Guid userId)
    {
        var transactions = await _rewardRepo.GetTransactionsByUserIdAsync(userId);
        var result = transactions.Select(t => new RewardTransactionResponse
        {
            Id = t.Id,
            Points = t.Points,
            Reason = t.Reason,
            Reference = t.Reference,
            CreatedAt = t.CreatedAt
        }).ToList();

        return ApiResponse<List<RewardTransactionResponse>>.Successfull("OK", result);
    }

    private static string CalculateTier(int points)
    {
        if (points >= GoldThreshold) return "Gold";
        if (points >= SilverThreshold) return "Silver";
        return "Bronze";
    }

    private static (string nextTier, int pointsToNext) GetNextTierInfo(string currentTier, int currentPoints) =>
        currentTier switch
        {
            "Bronze" => ("Silver", SilverThreshold - currentPoints),
            "Silver" => ("Gold", GoldThreshold - currentPoints),
            "Gold" => ("Top tier reached", 0),
            _ => ("Silver", SilverThreshold - currentPoints)
        };

    private static RewardResponse MapReward(Reward r)
    {
        var (nextTier, pointsToNext) = GetNextTierInfo(r.Tier, r.PointsBalance);
        return new RewardResponse
        {
            Id = r.Id,
            UserId = r.UserId,
            PointsBalance = r.PointsBalance,
            TotalEarned = r.TotalEarned,
            Tier = r.Tier,
            NextTier = nextTier,
            PointsToNext = pointsToNext,
            CreatedAt = r.CreatedAt
        };
    }
}
