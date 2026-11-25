using ITSupportSystem.Core.Entities;

namespace ITSupportSystem.Core.DTOs.Users;

public record UserResponse
{
    public string Id { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public DateTime DateCreated { get; init; }
    public bool IsLockedOut { get; init; }
    public string Role { get; init; } = string.Empty;

    public static UserResponse FromUser(ApplicationUser user, string role) => new()
    {
        Id = user.Id,
        UserName = user.UserName ?? string.Empty,
        Email = user.Email ?? string.Empty,
        FirstName = user.FirstName,
        LastName = user.LastName,
        PhoneNumber = user.PhoneNumber,
        DateCreated = user.DateCreated,
        IsLockedOut = user.LockoutEnd > DateTimeOffset.UtcNow,
        Role = role
    };
}