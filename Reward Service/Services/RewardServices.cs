using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using Reward_Service.Data;
using Reward_Service.DTOs;
using Reward_Service.Models;
using System.Threading.Channels;

namespace Reward_Service.Services;

public class RewardServices
{
    private readonly RewardDbContext _db;

    //Points per transfer
    private const int PointsPerTransfer = 10;
    private const int SilverThreshold = 1000;
    private const int GoldThreshold = 5000;

    public RewardServices(RewardDbContext db)
    {
        _db = db;
       
    }
    
    //Called by WalletService after a transfer completes
    public async Task<ApiResponse<RewardResponse>> AwardPointsAsync(AwardPointsRequest req)
    {
        //check
        var alreadyRewarded = await _db.RewardTransactions.AnyAsync(t => t.Reference == req.Reference&& t.UserId == req.UserId);

        if (alreadyRewarded)
        {
            return ApiResponse<RewardResponse>.Fail("Points already awarded for this transaction.");
        }

  
        var reward = await _db.Rewards.FirstOrDefaultAsync(r => r.UserId == req.UserId);

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
            _db.Rewards.Add(reward);
        }

      
        reward.PointsBalance += PointsPerTransfer;
        reward.TotalEarned += PointsPerTransfer;
        reward.UpdatedAt = DateTime.Now;


        var oldTier = reward.Tier;
        reward.Tier = CalculateTier(reward.PointsBalance);

     
        _db.RewardTransactions.Add(new RewardTransaction
        {
            Id = Guid.NewGuid(),
            UserId = req.UserId,
            Points = PointsPerTransfer,
            Reason = req.Reason,
            Reference = req.Reference,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return ApiResponse<RewardResponse>.Successfull($"Awarded {PointsPerTransfer} points.", MapReward(reward));
    }

   

    public async Task<ApiResponse<RewardResponse>> GetRewardsAsync(Guid userId)
    {
        var reward = await _db.Rewards.FirstOrDefaultAsync(r => r.UserId == userId);

        
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
        var transactions = await _db.RewardTransactions.Where(t => t.UserId == userId).OrderByDescending(t => t.CreatedAt).ToListAsync();

        var result = transactions.Select(t => new RewardTransactionResponse
        {
            Id = t.Id,
            Points = t.Points,
            Reason = t.Reason,
            Reference = t.Reference,
            CreatedAt = t.CreatedAt
        }).ToList();

        return ApiResponse<List<RewardTransactionResponse>>.Successfull("oK", result);
    }



    // Calculates tier based on points balance
    private static string CalculateTier(int points)
    {
        if (points >= GoldThreshold) return "Gold";
        if (points >= SilverThreshold) return "Silver";
        return "Bronze";
    }

    // points needed to reach next tier
    private static (string nextTier, int pointsToNext) GetNextTierInfo(string currentTier, int currentPoints)
    {
        return currentTier switch
        {
            "Bronze" => ("Silver", SilverThreshold - currentPoints),
            "Silver" => ("Gold", GoldThreshold - currentPoints),
            "Gold" => ("Top tier reached", 0),
            _ => ("Silver", SilverThreshold - currentPoints)
        };
    }

    // Maps Reward model → RewardResponse DTO
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
