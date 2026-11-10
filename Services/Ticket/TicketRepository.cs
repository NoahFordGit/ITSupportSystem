using Microsoft.EntityFrameworkCore;
using ITSupportSystem.Models.Entities;
using ITSupportSystem.Data;

namespace ITSupportSystem.Services;

public class TicketRepository : ITicketRepository
{

    // DbContext setup
    private readonly ApplicationDbContext _context;

    public TicketRepository(ApplicationDbContext context)
    {
        _context = context;
    } // End method


    // Create ticket method 
    public async Task<Ticket> CreateAsync(Ticket ticket)
    {
        _context.Ticket.Add(ticket);
        await _context.SaveChangesAsync();
        return ticket;
    } // End method


    // ReadAll tickets method
    public async Task<IEnumerable<Ticket>> ReadAllAsync()
    {
        return await _context.Ticket
            .ToListAsync();
    } // End method


    // Read(one) ticket method
    public async Task<Ticket?> ReadAsync(int TicketId)
    {
        return await _context.Ticket
            .FirstOrDefaultAsync(t => t.TicketId == TicketId);
    } // End method


    // Update ticket method
    public async Task<Ticket?> UpdateAsync(Ticket ticket)
    {
        var existing = await _context.Ticket.FindAsync(ticket.TicketId);
        if (existing == null)
        {
            return null;
        } // End if

        existing.Title = ticket.Title;
        existing.Description = ticket.Description;
        existing.Priority = ticket.Priority;
        existing.Status = ticket.Status;

        await _context.SaveChangesAsync();
        return existing;

    } // End method


    // Delete ticket method
    public async Task<bool> DeleteAsync(Ticket ticket)
    {
        var existing = await _context.Ticket.FindAsync(ticket.TicketId);
        if (existing == null)
        {
            return false;
        } // End if

        _context.Ticket.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    } // End method

} // End class