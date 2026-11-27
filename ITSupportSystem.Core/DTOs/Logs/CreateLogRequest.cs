using ITSupportSystem.Core.Entities;
using System.ComponentModel.DataAnnotations;


namespace ITSupportSystem.Core.DTOs.Logs;

public record CreateLogRequest
{
    [Required]
    public int? TicketId {get; set;}

    [Required, StringLength(256)]
    public string LogAction {get; set;} = string.Empty;

    [Required, StringLength(256)]
    public string Description {get; set;} = string.Empty;

} // End record