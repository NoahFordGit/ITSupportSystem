using ITSupportSystem.Models.Entities;

namespace ITSupportSystem.Services;

public interface ICommentRepository
{
    Task<IEnumerable<Comment>> ReadAllAsync();
    Task<Comment?> ReadAsync(int CommentId);
    Task<Comment?> CreateAsync(int CommentId, Comment comment);
    Task<Comment?> UpdateAsync(int oldId, Comment comment);
    Task<bool> DeleteAsync(Comment comment);
} // End interface