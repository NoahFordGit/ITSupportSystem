using ITSupportSystem.Core.Entities;

namespace ITSupportSystem.Core.DTOs.Comments;

public record CommentResponse
{
    public int CommentId {get; set;}

    public string Content {get; set;} = string.Empty;

    public DateTime CreatedOn {get; set;}

    // Ticket field
    public int TicketId {get; set;}

    // User field
    public string UserId {get; set;} = string.Empty;

    public static CommentResponse FromComment(Comment comment) => new()
    {
        CommentId = comment.CommentId,
        Content = comment.Content,
        CreatedOn = comment.CreatedOn,
        TicketId = comment.TicketId,
        UserId = comment.UserId
    }; // End static

} // End record