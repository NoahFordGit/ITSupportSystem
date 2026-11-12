using ITSupportSystem.Data;
using ITSupportSystem.Models.Entities;

namespace ITSupportSystem.Services;

public class LogRepository : ILogRepository
{

    // DbContext setup
    private readonly ApplicationDbContext _context;
    
    public LogRepository(ApplicationDbContext context)
    {
        _context = context;
    } // End method

// NOTE: all commented code blocks (unless otherwise stated) are related to the ApplicationUser model.
//      These blocks have been commented out for now to avoid errors but need to be uncommented at some point in time.
    
    // Create log method
    public async Task<Log?> CreateAsync(int TicketId, string Id, Log log)
    {
        var ticket = await _context.Ticket.FindAsync(TicketId);
        if (ticket == null)
        {
            return null;
        } // End if 

        // var user = await _context.ApplicationUser.FindAsync(Id);
        // if (user == null)
        // {
        //     return null;
        // } // End if

        log.TicketId = ticket.TicketId;
        log.Ticket = ticket;
        // log.Id = applictionUser.Id;
        // log.ApplicationUser = applicationUser;

        // _context.Comment.Add(comment);
        await _context.SaveChangesAsync();
        return log;
    } // End method

    // Read all logs method
    public async Task<IEnumerable<Log>> ReadAllAsync()
    {
        // return await _context.Log
        //     .Include(c => c.Ticket)
        //     .Include(c => c.ApplicationUser)
        //     .ToListAsync();

        throw new NotImplementedException();
    } // End method

    // Read(one) log method
    public async Task<Log?> ReadAsync(int LogId)
    {
        // return await _context.Log
        //     .Include(c => c.Ticket)
        //     .Include(c => c.ApplicationUser)
        //     .FirstOrDefaultAsync(c => c.);
        throw new NotImplementedException();
    } // End method

    // Update comment method
    public async Task<Log?> UpdateAsync(int oldId, Log log)
    {
        var existing = await _context.Log.FindAsync(oldId, log.LogId);
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
        var existing = await _context.Log.FindAsync(log.LogId);
        if (existing == null)
        {
            return false;
        } // End if

        _context.Log.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    } // End method

} // End class