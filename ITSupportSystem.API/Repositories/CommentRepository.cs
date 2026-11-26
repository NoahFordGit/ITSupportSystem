using Microsoft.EntityFrameworkCore;
using ITSupportSystem.API.Data;
using ITSupportSystem.Core.Entities;
using ITSupportSystem.Core.Interfaces;

namespace ITSupportSystem.API.Repositories;

public class CommentRepository(ApplicationDbContext context) : ICommentRepository
{

    // DbContext setup
    private readonly ApplicationDbContext _context = context;


    // Create comment method
    public async Task<Comment?> CreateAsync(int TicketId, string Id, Comment comment)
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

        comment.TicketId = ticket.TicketId;
        comment.Ticket = ticket;
        comment.UserId = user.Id;
        comment.ApplicationUser = user;

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();
        return comment;

    } // End method

    // Read all comments method
    public async Task<IEnumerable<Comment>> ReadAllAsync()
    {
        return await _context.Comments
            .Include(c => c.Ticket)
            .Include(c => c.ApplicationUser)
            .ToListAsync();
    } // End method


    // Read(one) comment method
    public async Task<Comment?> ReadAsync(int CommentId)
    {
        return await _context.Comments
            .Include(c => c.Ticket)
            .Include(c => c.ApplicationUser)
            .FirstOrDefaultAsync(c => c.CommentId == CommentId);
    } // End method


    // Update comment mehtod
    public async Task<Comment?> UpdateAsync(int oldId, Comment comment)
    {
        var existing = await _context.Comments.FindAsync(oldId, comment.CommentId);
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
        var existing = await _context.Comments.FindAsync(comment.CommentId);
        if (existing == null)
        {
            return false;
        } // End if

        _context.Comments.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    } // End method

} // End class