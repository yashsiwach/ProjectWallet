using AdminService.Application.Interfaces;
using AdminService.Domain.Models;
using AdminService.DTOs;

namespace AdminService.Application.Services;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepo;
    private readonly ILogger<TicketService> _logger;

    public TicketService(ITicketRepository ticketRepo, ILogger<TicketService> logger)
    {
        _ticketRepo = ticketRepo;
        _logger = logger;
    }

    public async Task<ApiResponse<TicketResponse>> CreateTicketAsync(Guid userId, string userEmail, CreateTicketRequest req)
    {
        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserEmail = userEmail,
            Subject = req.Subject.Trim(),
            Message = req.Message.Trim(),
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };

        await _ticketRepo.AddAsync(ticket);
        await _ticketRepo.SaveChangesAsync();

        _logger.LogInformation("Ticket created by {Email}", userEmail);

        return ApiResponse<TicketResponse>.Successfull("Ticket submitted successfully.", MapTicket(ticket));
    }

    public async Task<ApiResponse<List<TicketResponse>>> GetMyTicketsAsync(Guid userId)
    {
        var tickets = await _ticketRepo.GetByUserIdAsync(userId);
        return ApiResponse<List<TicketResponse>>.Successfull("OK", tickets.Select(MapTicket).ToList());
    }

    public async Task<ApiResponse<List<TicketResponse>>> GetTicketsAsync(string? status)
    {
        var tickets = await _ticketRepo.GetByStatusAsync(status);
        return ApiResponse<List<TicketResponse>>.Successfull("OK", tickets.Select(MapTicket).ToList());
    }

    public async Task<ApiResponse<string>> ReplyToTicketAsync(Guid ticketId, TicketReplyRequest req, Guid adminId)
    {
        var ticket = await _ticketRepo.FindByIdAsync(ticketId);
        if (ticket == null) return ApiResponse<string>.Fail("Ticket not found.");
        if (ticket.Status == "Closed") return ApiResponse<string>.Fail("Ticket is already closed.");

        ticket.AdminReply = req.Reply;
        ticket.RespondedBy = adminId;
        ticket.RespondedAt = DateTime.Now;
        ticket.Status = "Responded";
        await _ticketRepo.SaveChangesAsync();

        _logger.LogInformation("Ticket {TicketId} replied by admin {AdminId}", ticketId, adminId);

        return ApiResponse<string>.Successfull("Reply sent successfully.", "");
    }

    private static TicketResponse MapTicket(SupportTicket t) => new()
    {
        Id = t.Id,
        UserId = t.UserId,
        UserEmail = t.UserEmail,
        Subject = t.Subject,
        Message = t.Message,
        Status = t.Status,
        AdminReply = t.AdminReply,
        CreatedAt = t.CreatedAt,
        RespondedAt = t.RespondedAt
    };
}
