using System.ComponentModel.DataAnnotations;

namespace ITSupportSystem.Core.DTOs.Users;

public record DeleteUserRequest
{
    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    // Keep the password out of logs
    protected virtual bool PrintMembers(System.Text.StringBuilder builder) => false;
}