using PaymentService.Domain.Models;

namespace PaymentService.Application.Interfaces;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment);
    Task<List<Payment>> GetByUserIdAsync(Guid userId);
    Task SaveChangesAsync();
}
