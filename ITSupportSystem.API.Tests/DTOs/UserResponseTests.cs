using ITSupportSystem.Core.Constants;
using ITSupportSystem.Core.DTOs.Users;
using ITSupportSystem.Core.Entities;

namespace ITSupportSystem.API.Tests.DTOs;

/// <summary>Plain unit tests for UserResponse.FromUser: no web server involved.</summary>
public class UserResponseTests
{
    private static ApplicationUser NewUser() => new()
    {
        UserName = "jdoe",
        Email = "jdoe@example.com",
        FirstName = "Jane",
        LastName = "Doe",
        PhoneNumber = "423-555-0100",
        DateCreated = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
        PasswordHash = "secret-hash"
    };

    [Fact]
    public void FromUser_CopiesEveryField()
    {
        var user = NewUser();

        var response = UserResponse.FromUser(user, Roles.Staff);

        Assert.Equal(user.Id, response.Id);
        Assert.Equal("jdoe", response.UserName);
        Assert.Equal("jdoe@example.com", response.Email);
        Assert.Equal("Jane", response.FirstName);
        Assert.Equal("Doe", response.LastName);
        Assert.Equal("423-555-0100", response.PhoneNumber);
        Assert.Equal(user.DateCreated, response.DateCreated);
        Assert.Equal(Roles.Staff, response.Role);
        Assert.False(response.IsLockedOut);
    }

    [Fact]
    public void FromUser_NullUserNameAndEmail_BecomeEmptyStrings()
    {
        var user = NewUser();
        user.UserName = null;
        user.Email = null;

        var response = UserResponse.FromUser(user, Roles.User);

        Assert.Equal(string.Empty, response.UserName);
        Assert.Equal(string.Empty, response.Email);
    }

    [Fact]
    public void FromUser_NullPhone_StaysNull()
    {
        var user = NewUser();
        user.PhoneNumber = null;

        Assert.Null(UserResponse.FromUser(user, Roles.User).PhoneNumber);
    }

    [Fact]
    public void FromUser_LockoutEndInFuture_IsLockedOut()
    {
        var user = NewUser();
        user.LockoutEnd = DateTimeOffset.UtcNow.AddHours(1);

        Assert.True(UserResponse.FromUser(user, Roles.User).IsLockedOut);
    }

    [Fact]
    public void FromUser_LockoutEndInPast_IsNotLockedOut()
    {
        var user = NewUser();
        user.LockoutEnd = DateTimeOffset.UtcNow.AddHours(-1);

        Assert.False(UserResponse.FromUser(user, Roles.User).IsLockedOut);
    }

    [Fact]
    public void FromUser_NoLockoutEnd_IsNotLockedOut()
    {
        var user = NewUser();
        user.LockoutEnd = null;

        Assert.False(UserResponse.FromUser(user, Roles.User).IsLockedOut);
    }

    [Fact]
    public void UserResponse_HasNoSecurityProperties()
    {
        var names = typeof(UserResponse).GetProperties().Select(p => p.Name).ToList();

        Assert.DoesNotContain("PasswordHash", names);
        Assert.DoesNotContain("SecurityStamp", names);
        Assert.DoesNotContain("ConcurrencyStamp", names);
    }
}
