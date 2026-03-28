using AdminService.Application.Interfaces;
using AdminService.Domain.Models;
using AdminService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Infrastructure.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly AdminDbContext _db;

    public TicketRepository(AdminDbContext db) => _db = db;

    public Task<SupportTicket?> FindByIdAsync(Guid id) =>
        _db.SupportTickets.FirstOrDefaultAsync(t => t.Id == id);

    public Task<List<SupportTicket>> GetByStatusAsync(string? status)
    {
        var query = _db.SupportTickets.AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);
        return query.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public Task<List<SupportTicket>> GetByUserIdAsync(Guid userId) =>
        _db.SupportTickets.Where(t => t.UserId == userId).OrderByDescending(t => t.CreatedAt).ToListAsync();

    public async Task AddAsync(SupportTicket ticket) => await _db.SupportTickets.AddAsync(ticket);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
