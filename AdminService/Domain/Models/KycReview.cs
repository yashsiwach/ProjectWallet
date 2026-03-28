namespace AdminService.Domain.Models;

public partial class KycReview
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = null!;
    public string UserEmail { get; set; } = null!;
    public string DocumentType { get; set; } = null!;
    public string DocumentNumber { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? AdminNote { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
