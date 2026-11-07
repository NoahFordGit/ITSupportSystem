using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ITSupportSystem.Models.Entities;

public class ApplicationUser : IdentityUser
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;
    [Required]

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}