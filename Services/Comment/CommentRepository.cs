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

    public Task<Comment?> CreateAsync(int CommentId, Comment comment)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteAsync(Comment comment)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Comment>> ReadAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<Comment?> ReadAsync(int CommentId)
    {
        throw new NotImplementedException();
    }

    public Task<Comment?> UpdateAsync(int oldId, Comment comment)
    {
        throw new NotImplementedException();
    }
}