using ITSupportSystem.Core.Entities;

namespace ITSupportSystem.Core.DTOs.Tickets;

public record TicketResponse
{
    public int TicketId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public Status Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string UserId { get; set; } = string.Empty;

    public static TicketResponse FromTicket(Ticket ticket) => new()
    {
        TicketId = ticket.TicketId,
        Title = ticket.Title,
        Description = ticket.Description,
        Priority = ticket.Priority,
        Status = ticket.Status,
        CreatedAt = ticket.CreatedAt,
        UpdatedAt = ticket.UpdatedAt,
        UserId = ticket.UserId
    };
}