using System.ComponentModel.DataAnnotations;
using ITSupportSystem.Core.Entities;

namespace ITSupportSystem.Core.DTOs.Comments;

public record UpdateCommentRequest
{
    [Required, StringLength(256)]
    public string Content {get; set;} = string.Empty;
    
} // End record