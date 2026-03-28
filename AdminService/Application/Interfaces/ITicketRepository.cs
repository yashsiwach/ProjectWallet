using AdminService.Domain.Models;

namespace AdminService.Application.Interfaces;

public interface ITicketRepository
{
    Task<SupportTicket?> FindByIdAsync(Guid id);
    Task<List<SupportTicket>> GetByStatusAsync(string? status);
    Task<List<SupportTicket>> GetByUserIdAsync(Guid userId);
    Task AddAsync(SupportTicket ticket);
    Task SaveChangesAsync();
}
