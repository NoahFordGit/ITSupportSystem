using Microsoft.EntityFrameworkCore;
using ITSupportSystem.Core.Entities;
using ITSupportSystem.API.Data;
using ITSupportSystem.Core.Interfaces;

namespace ITSupportSystem.API.Repositories;

public class LogRepository(ApplicationDbContext context) : ILogRepository
{

    // DbContext setup
    private readonly ApplicationDbContext _context = context;


    // Create log method
    public async Task<Log?> CreateAsync(int TicketId, string Id, Log log)
    {
        var ticket = await _context.Tickets.FindAsync(TicketId);
        if (ticket == null)
        {
            return null;
        } // End if 

        var user = await _context.Users.FindAsync(Id);
        if (user == null)
        {
            return null;
        } // End if

        log.TicketId = ticket.TicketId;
        log.Ticket = ticket;
        log.UserId = user.Id;
        log.User = user;

        _context.Logs.Add(log);
        await _context.SaveChangesAsync();
        return log;
    } // End method

    // Read all logs method
    public async Task<IEnumerable<Log>> ReadAllAsync()
    {
        return await _context.Logs
            .Include(l => l.Ticket)
            .Include(l => l.User)
            .ToListAsync();
    } // End method

    public async Task<IEnumerable<Log>> ReadByTicketAsync(int ticketId)
    {
        return await _context.Logs
            .Where(l => l.TicketId == ticketId)
            .OrderBy(l => l.LoggedOn)
            .ToListAsync();
    }

    // Read(one) log method
    public async Task<Log?> ReadAsync(int LogId)
    {
        return await _context.Logs
            .Include(l => l.Ticket)
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.LogId == LogId);
    } // End method

    // Update log method
    public async Task<Log?> UpdateAsync(int oldId, Log log)
    {
        var existing = await _context.Logs.FindAsync(oldId);
        if (existing == null)
        {
            return null;
        } // End if

        existing.LogAction = log.LogAction;
        existing.Description = log.Description;
        existing.LoggedOn = log.LoggedOn;
        await _context.SaveChangesAsync();
        return existing;
    } // End method

    // Delete log method
    public async Task<bool> DeleteAsync(Log log)
    {
        var existing = await _context.Logs.FindAsync(log.LogId);
        if (existing == null)
        {
            return false;
        } // End if

        _context.Logs.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    } // End method

} // End class