using ITSupportSystem.Data;
using ITSupportSystem.Models.Entities;

namespace ITSupportSystem.Services;

public class CommentRepository : ICommentRepository
{

    // DbContext setup
    private readonly ApplicationDbContext _context;
    
    public CommentRepository(ApplicationDbContext context)
    {
        _context = context;
    } // End method


// NOTE: all commented code blocks (unless otherwise stated) are related to the ApplicationUser model.
//          These blocks have been commented out for now to avoid errors but need to be uncommented at some point in time.

    // Create comment method
    public async Task<Comment?> CreateAsync(int TicketId, string Id, Comment comment)
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

        comment.TicketId = ticket.TicketId;
        comment.Ticket = ticket;
        // comment.Id = applictionUser.Id;
        // comment.ApplicationUser = applicationUser;

        // _context.Comment.Add(comment);
        await _context.SaveChangesAsync();
        return comment;

    } // End method

    // Read all comments method
    public async Task<IEnumerable<Comment>> ReadAllAsync()
    {
        // return await _context.Comment
        //     .Include(c => c.Ticket)
        //     .Include(c => c.ApplicationUser)
        //     .ToListAsync();
        throw new NotImplementedException();
    } // End method


    // Read(one) comment method
    public async Task<Comment?> ReadAsync(int CommentId)
    {
        // return await _context.Comment
        //     .Include(c => c.Ticket)
        //     .Include(c => c.ApplicationUser)
        //     .FirstOrDefaultAsync(c => c.);
         throw new NotImplementedException();
    } // End method


    // Update comment mehtod
    public async Task<Comment?> UpdateAsync(int oldId, Comment comment)
    {
        var existing = await _context.Comment.FindAsync(oldId, comment.CommentId);
        if (existing == null)
        {
            return null;
        } // End if

        existing.Content = comment.Content;
        existing.CreatedOn = comment.CreatedOn;
        await _context.SaveChangesAsync();
        return existing;
    } // End method


    // Delete comment method
    public async Task<bool> DeleteAsync(Comment comment)
    {
        var existing = await _context.Comment.FindAsync(comment.CommentId);
        if (existing == null)
        {
            return false;
        } // End if

        _context.Comment.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    } // End method

} // End class