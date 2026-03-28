using AdminService.DTOs;

namespace AdminService.Application.Interfaces;

public interface ITicketService
{
    Task<ApiResponse<TicketResponse>> CreateTicketAsync(Guid userId, string userEmail, CreateTicketRequest req);
    Task<ApiResponse<List<TicketResponse>>> GetMyTicketsAsync(Guid userId);
    Task<ApiResponse<List<TicketResponse>>> GetTicketsAsync(string? status);
    Task<ApiResponse<string>> ReplyToTicketAsync(Guid ticketId, TicketReplyRequest req, Guid adminId);
}
