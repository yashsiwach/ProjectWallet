namespace Reward_Service.Domain.Models;

public partial class Reward
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int PointsBalance { get; set; }
    public int TotalEarned { get; set; }
    public string Tier { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
