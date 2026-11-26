using System.ComponentModel.DataAnnotations;
using ITSupportSystem.Core.Constants;
namespace ITSupportSystem.Core.DTOs.Users;

public record ChangeRoleRequest
{
    [Required, AllowedValues(Roles.Admin, Roles.Staff, Roles.User)]
    public string Role { get; set; } = string.Empty;
}