using System.ComponentModel.DataAnnotations;
using ITSupportSystem.Core.Entities;

namespace ITSupportSystem.Core.DTOs.Tickets;

public record UpdateTicketRequest
{
    [Required, StringLength(256)]
    public string Title { get; set; } = string.Empty;
    [Required, StringLength(256)]
    public string Description { get; set; } = string.Empty;
    [Required, EnumDataType(typeof(Priority))]
    public Priority? Priority { get; set; }
}