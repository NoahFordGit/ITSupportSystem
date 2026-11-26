using System.ComponentModel.DataAnnotations;
using ITSupportSystem.Core.Entities;

namespace ITSupportSystem.Core.DTOs.Tickets;

public record UpdateTicketStatusRequest
{
    [Required, EnumDataType(typeof(Status))]
    public Status? Status { get; set; }
}