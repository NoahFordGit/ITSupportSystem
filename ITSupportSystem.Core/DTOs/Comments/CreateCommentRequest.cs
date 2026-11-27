using ITSupportSystem.Core.Entities;
using System.ComponentModel.DataAnnotations;


namespace ITSupportSystem.Core.DTOs.Comments;

public record CreateCommentRequest
{
    [Required, StringLength(256)]
    public string Content {get; set;} = string.Empty;

} // End record