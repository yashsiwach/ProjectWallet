namespace PaymentService.Domain.Models;

public partial class Payment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid WalletId { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? GatewayRef { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
