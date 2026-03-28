using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Interfaces;
using PaymentService.Domain.Models;
using PaymentService.Infrastructure.Data;

namespace PaymentService.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _db;

    public PaymentRepository(PaymentDbContext db) => _db = db;

    public async Task AddAsync(Payment payment) => await _db.Payments.AddAsync(payment);

    public Task<List<Payment>> GetByUserIdAsync(Guid userId) =>
        _db.Payments.Where(p => p.UserId == userId).OrderByDescending(p => p.CreatedAt).ToListAsync();

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
