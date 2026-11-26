using Microsoft.EntityFrameworkCore;
using ITSupportSystem.Core.Entities;
using ITSupportSystem.API.Data;
using ITSupportSystem.Core.Interfaces;

namespace ITSupportSystem.API.Repositories;

public class TicketRepository(ApplicationDbContext context) : ITicketRepository
{

    // DbContext setup
    private readonly ApplicationDbContext _context = context;


    // Create ticket method 
    public async Task<Ticket> CreateAsync(Ticket ticket)
    {
        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();
        return ticket;
    } // End method


    // ReadAll tickets method
    public async Task<IEnumerable<Ticket>> ReadAllAsync()
    {
        return await _context.Tickets
            .ToListAsync();
    } // End method


    // Read(one) ticket method
    public async Task<Ticket?> ReadAsync(int TicketId)
    {
        return await _context.Tickets
            .FirstOrDefaultAsync(t => t.TicketId == TicketId);
    } // End method


    // Update ticket method
    public async Task<Ticket?> UpdateAsync(Ticket ticket)
    {
        var existing = await _context.Tickets.FindAsync(ticket.TicketId);
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
        var existing = await _context.Tickets.FindAsync(ticket.TicketId);
        if (existing == null)
        {
            return false;
        } // End if

        _context.Tickets.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    } // End method

} // End class