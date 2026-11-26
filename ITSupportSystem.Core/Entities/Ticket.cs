using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
// using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ITSupportSystem.Core.Entities;

// Enum for priority field
[JsonConverter(typeof(JsonStringEnumConverter<Priority>))]
public enum Priority
{
    Low, 
    Medium, 
    High, 
    Critical
} // End enum


// Enum for status field
[JsonConverter(typeof(JsonStringEnumConverter<Status>))]
public enum Status
{
    New, 
    Open,
    [Display(Name = "In Progress")]     // Display name is used here just so it looks nicer
    InProgress,
    Pending,
    Resolved,
    Closed
} // End enum


// Enum for category field
[JsonConverter(typeof(JsonStringEnumConverter<Category>))]
public enum Category
{
    Hardware,
    Software,
    Network,
    Security,
    Access,
    Services
} // End enum


// Class for Ticket table
public class Ticket
{
    [Key]
    public int TicketId {get; set;}

    [Required]
    [StringLength(256)]
    public string Title {get; set;} = string.Empty;

    [StringLength(256)]
    public string Description {get; set;} = string.Empty;

    [Required]
    public Priority Priority {get; set;}

    [Required]
    public Status Status {get; set;}

    [Required, DataType(DataType.Date)]
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;

    [Required, DataType(DataType.Date)]
    public DateTime UpdatedAt {get; set;} = DateTime.UtcNow;

    public ICollection<Comment> Comment {get; set;} = new List<Comment>();

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? CreatedBy { get; set; }
} // End class