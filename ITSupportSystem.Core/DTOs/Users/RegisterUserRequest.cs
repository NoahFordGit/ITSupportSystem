using System.ComponentModel.DataAnnotations;

namespace ITSupportSystem.Core.DTOs.Users;

public record RegisterUserRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string UserName { get; init; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [MinLength(8), MaxLength(100)]
    public string Password { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; init; }

    // Keep the password out of logs
    protected virtual bool PrintMembers(System.Text.StringBuilder builder)
    {
        builder.Append($"Email = {Email}, UserName = {UserName}, FirstName = {FirstName}, LastName = {LastName}");
        return true;
    }
}