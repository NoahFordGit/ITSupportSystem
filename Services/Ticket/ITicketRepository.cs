using ITSupportSystem.Models.Entities;

namespace ITSupportSystem.Services;

public interface ITicketRepository
{
    Task<IEnumerable<Ticket>> ReadAllAsync();
    Task<Ticket?> ReadAsync(int TicketId);
    Task<Ticket> CreateAsync(Ticket ticket);
    Task<Ticket?> UpdateAsync(Ticket ticket);
    Task<bool> DeleteAsync(Ticket ticket);
} // End interface