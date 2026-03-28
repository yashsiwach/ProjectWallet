using Reward_Service.DTOs;

namespace Reward_Service.Application.Interfaces;

public interface IRewardService
{
    Task<ApiResponse<RewardResponse>> AwardPointsAsync(AwardPointsRequest req);
    Task<ApiResponse<RewardResponse>> GetRewardsAsync(Guid userId);
    Task<ApiResponse<List<RewardTransactionResponse>>> GetHistoryAsync(Guid userId);
}
