using System.ComponentModel.DataAnnotations;
using ITSupportSystem.Core.Constants;

namespace ITSupportSystem.Core.DTOs.Users;

public record UpdateUserRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; init; }
}