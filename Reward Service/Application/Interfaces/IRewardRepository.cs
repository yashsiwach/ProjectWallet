using Reward_Service.Domain.Models;

namespace Reward_Service.Application.Interfaces;

public interface IRewardRepository
{
    Task<bool> IsAlreadyRewardedAsync(Guid userId, string reference);
    Task<Reward?> FindByUserIdAsync(Guid userId);
    Task AddAsync(Reward reward);
    Task AddTransactionAsync(RewardTransaction transaction);
    Task<List<RewardTransaction>> GetTransactionsByUserIdAsync(Guid userId);
    Task SaveChangesAsync();
}
