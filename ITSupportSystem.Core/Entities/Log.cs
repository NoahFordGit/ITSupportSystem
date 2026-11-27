using System.ComponentModel.DataAnnotations;
// using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ITSupportSystem.Core.Entities;

// Possible Log table class
public class Log
{
    [Key]
    public int LogId {get; set;}

    [StringLength(256)]
    public string LogAction {get; set;} = string.Empty;

    [StringLength(256)]
    public string Description {get; set;} = string.Empty;

    [DataType(DataType.Date)]
    public DateTime LoggedOn {get; set;} = DateTime.UtcNow;

    // Ticket fields
    public int TicketId {get; set;}
    public Ticket? Ticket {get; set;}

    // User fields
    public string UserId {get; set;} = string.Empty;
    public ApplicationUser? User {get; set;}

} // End class