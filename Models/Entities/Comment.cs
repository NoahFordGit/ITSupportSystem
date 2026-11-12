using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ITSupportSystem.Models.Entities;

// Comment is a LINKING entity between Ticket and User (but only if user has a staff or admin role)
// To make this simplier, Comment has a standalone PK with TicketId and User being FKs only
public class Comment
{
    [Key]
    public int CommentId {get; set;}

    [StringLength(256)]
    public string Content {get; set;} = string.Empty;

    [DataType(DataType.Date)]
    public DateTime CreatedOn {get; set;}

    // Ticket fields
    public int TicketId {get; set;}
    public Ticket? Ticket {get; set;}

    // User fields (uncomment AFTER user is made)
    // public string Id {get; set;}
    // public User? ApplicationUser {get; set;}



} // End class