using Microsoft.EntityFrameworkCore;
using Reward_Service.Application.Interfaces;
using Reward_Service.Domain.Models;
using Reward_Service.Infrastructure.Data;

namespace Reward_Service.Infrastructure.Repositories;

public class RewardRepository : IRewardRepository
{
    private readonly RewardDbContext _db;

    public RewardRepository(RewardDbContext db) => _db = db;

    public Task<bool> IsAlreadyRewardedAsync(Guid userId, string reference) =>
        _db.RewardTransactions.AnyAsync(t => t.Reference == reference && t.UserId == userId);

    public Task<Reward?> FindByUserIdAsync(Guid userId) =>
        _db.Rewards.FirstOrDefaultAsync(r => r.UserId == userId);

    public async Task AddAsync(Reward reward) => await _db.Rewards.AddAsync(reward);

    public async Task AddTransactionAsync(RewardTransaction transaction) =>
        await _db.RewardTransactions.AddAsync(transaction);

    public Task<List<RewardTransaction>> GetTransactionsByUserIdAsync(Guid userId) =>
        _db.RewardTransactions.Where(t => t.UserId == userId).OrderByDescending(t => t.CreatedAt).ToListAsync();

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
