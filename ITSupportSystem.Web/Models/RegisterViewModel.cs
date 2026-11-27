using System.ComponentModel.DataAnnotations;

namespace ITSupportSystem.Web.Models;

public class RegisterViewModel
{
    [Required, MaxLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}