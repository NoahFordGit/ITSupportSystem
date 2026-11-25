using System.ComponentModel.DataAnnotations;
using ITSupportSystem.Core.Constants;

namespace ITSupportSystem.Core.DTOs.Users;

public record CreateUserRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;
    [Required, MaxLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [MinLength(8), MaxLength(100)]
    public string Password { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; init; }

    [Required, AllowedValues(Roles.Admin, Roles.Staff, Roles.User)]
    public string Role { get; init; } = string.Empty;

    // Records auto-generate ToString() with every property, including Password.
    // Override it so the password never ends up in logs.
    protected virtual bool PrintMembers(System.Text.StringBuilder builder)
    {
        builder.Append($"Email = {Email}, FirstName = {FirstName}, LastName = {LastName}, Role = {Role}");
        return true;
    }
}