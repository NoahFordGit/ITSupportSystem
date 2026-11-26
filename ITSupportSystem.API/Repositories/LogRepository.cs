using Microsoft.EntityFrameworkCore;
using ITSupportSystem.Core.Entities;
using ITSupportSystem.API.Data;
using ITSupportSystem.Core.Interfaces;

namespace ITSupportSystem.API.Repositories;

public class LogRepository(ApplicationDbContext context) : ILogRepository
{

    // DbContext setup
    private readonly ApplicationDbContext _context = context;

    // NOTE: all commented code blocks (unless otherwise stated) are related to the ApplicationUser model.
    //      These blocks have been commented out for now to avoid errors but need to be uncommented at some point in time.

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
        log.ApplicationUser = user;

        _context.Logs.Add(log);
        await _context.SaveChangesAsync();
        return log;
    } // End method

    // Read all logs method
    public async Task<IEnumerable<Log>> ReadAllAsync()
    {
        return await _context.Logs
            .Include(l => l.Ticket)
            .Include(l => l.ApplicationUser)
            .ToListAsync();

        throw new NotImplementedException();
    } // End method

    // Read(one) log method
    public async Task<Log?> ReadAsync(int LogId)
    {
        return await _context.Logs
            .Include(l => l.Ticket)
            .Include(l => l.ApplicationUser)
            .FirstOrDefaultAsync(l => l.LogId == LogId);
    } // End method

    // Update log method
    public async Task<Log?> UpdateAsync(int oldId, Log log)
    {
        var existing = await _context.Logs.FindAsync(oldId, log.LogId);
        if (existing == null)
        {
            return null;
        } // End if

        existing.LogAction = log.LogAction;
        existing.Description = log.Description;
        existing.LoggedOn = log.LoggedOn;
        await _context.SaveChangesAsync();
        return existing;

        throw new NotImplementedException();
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