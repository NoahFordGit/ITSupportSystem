using System.ComponentModel.DataAnnotations;
using ITSupportSystem.Core.Constants;
using ITSupportSystem.Core.DTOs.Users;

namespace ITSupportSystem.API.Tests.DTOs;

/// <summary>
/// Unit tests for the request DTOs' validation attributes and ToString redaction.
/// Runs the same DataAnnotations validator that [ApiController] uses.
/// </summary>
public class RequestDtoTests
{
    private const string Password = "Passw0rd!";

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static IEnumerable<string> FailedMembers(object model) =>
        Validate(model).SelectMany(r => r.MemberNames);

    private static CreateUserRequest ValidCreate() => new()
    {
        Email = "a@example.com",
        UserName = "a",
        Password = Password,
        FirstName = "A",
        LastName = "B",
        PhoneNumber = "423-555-0100",
        Role = Roles.User
    };

    private static RegisterUserRequest ValidRegister() => new()
    {
        Email = "a@example.com",
        UserName = "a",
        Password = Password,
        FirstName = "A",
        LastName = "B",
        PhoneNumber = "423-555-0100"
    };

    private static UpdateUserRequest ValidUpdate() => new()
    {
        Email = "a@example.com",
        UserName = "a",
        FirstName = "A",
        LastName = "B"
    };

    // ---------- CreateUserRequest ----------

    [Fact]
    public void Create_Valid_HasNoErrors() => Assert.Empty(Validate(ValidCreate()));

    [Fact]
    public void Create_NoPhone_IsValid() => Assert.Empty(Validate(ValidCreate() with { PhoneNumber = null }));

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Staff)]
    [InlineData(Roles.User)]
    public void Create_EveryKnownRole_IsValid(string role) =>
        Assert.Empty(Validate(ValidCreate() with { Role = role }));

    [Theory]
    [InlineData("SuperAdmin")]
    [InlineData("admin")] // case matters
    [InlineData("")]
    public void Create_UnknownRole_FailsOnRole(string role) =>
        Assert.Contains(nameof(CreateUserRequest.Role), FailedMembers(ValidCreate() with { Role = role }));

    [Fact]
    public void Create_BadEmail_FailsOnEmail() =>
        Assert.Contains(nameof(CreateUserRequest.Email), FailedMembers(ValidCreate() with { Email = "nope" }));

    [Fact]
    public void Create_ShortPassword_FailsOnPassword() =>
        Assert.Contains(nameof(CreateUserRequest.Password), FailedMembers(ValidCreate() with { Password = "Ab1!" }));

    [Fact]
    public void Create_TooLongPassword_FailsOnPassword() =>
        Assert.Contains(nameof(CreateUserRequest.Password), FailedMembers(ValidCreate() with { Password = new string('a', 101) }));

    [Fact]
    public void Create_TooLongFirstName_FailsOnFirstName() =>
        Assert.Contains(nameof(CreateUserRequest.FirstName), FailedMembers(ValidCreate() with { FirstName = new string('a', 101) }));

    [Fact]
    public void Create_BadPhone_FailsOnPhone() =>
        Assert.Contains(nameof(CreateUserRequest.PhoneNumber), FailedMembers(ValidCreate() with { PhoneNumber = "call me maybe" }));

    [Fact]
    public void Create_ToString_DoesNotLeakPassword() =>
        Assert.DoesNotContain(Password, ValidCreate().ToString());

    // ---------- RegisterUserRequest ----------

    [Fact]
    public void Register_Valid_HasNoErrors() => Assert.Empty(Validate(ValidRegister()));

    [Fact]
    public void Register_HasNoRoleProperty() =>
        Assert.Null(typeof(RegisterUserRequest).GetProperty("Role")); // can't self-assign a role

    [Theory]
    [InlineData(nameof(RegisterUserRequest.Email))]
    [InlineData(nameof(RegisterUserRequest.UserName))]
    [InlineData(nameof(RegisterUserRequest.Password))]
    [InlineData(nameof(RegisterUserRequest.FirstName))]
    [InlineData(nameof(RegisterUserRequest.LastName))]
    public void Register_MissingRequiredField_FailsOnThatField(string field)
    {
        var request = field switch
        {
            nameof(RegisterUserRequest.Email) => ValidRegister() with { Email = "" },
            nameof(RegisterUserRequest.UserName) => ValidRegister() with { UserName = "" },
            nameof(RegisterUserRequest.Password) => ValidRegister() with { Password = "" },
            nameof(RegisterUserRequest.FirstName) => ValidRegister() with { FirstName = "" },
            _ => ValidRegister() with { LastName = "" },
        };

        Assert.Contains(field, FailedMembers(request));
    }

    [Fact]
    public void Register_ToString_DoesNotLeakPassword()
    {
        var text = ValidRegister().ToString();

        Assert.DoesNotContain(Password, text);
        Assert.Contains("a@example.com", text); // still useful for logs
    }

    // ---------- UpdateUserRequest ----------

    [Fact]
    public void Update_Valid_HasNoErrors() => Assert.Empty(Validate(ValidUpdate()));

    [Fact]
    public void Update_HasNoPasswordOrRoleProperty()
    {
        Assert.Null(typeof(UpdateUserRequest).GetProperty("Password"));
        Assert.Null(typeof(UpdateUserRequest).GetProperty("Role"));
    }

    [Fact]
    public void Update_BadEmail_FailsOnEmail() =>
        Assert.Contains(nameof(UpdateUserRequest.Email), FailedMembers(ValidUpdate() with { Email = "nope" }));

    [Fact]
    public void Update_TooLongUserName_FailsOnUserName() =>
        Assert.Contains(nameof(UpdateUserRequest.UserName), FailedMembers(ValidUpdate() with { UserName = new string('a', 101) }));

    // ---------- ChangeRoleRequest ----------

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Staff)]
    [InlineData(Roles.User)]
    public void ChangeRole_KnownRole_IsValid(string role) =>
        Assert.Empty(Validate(new ChangeRoleRequest { Role = role }));

    [Theory]
    [InlineData("Owner")]
    [InlineData("")]
    public void ChangeRole_UnknownRole_FailsOnRole(string role) =>
        Assert.Contains(nameof(ChangeRoleRequest.Role), FailedMembers(new ChangeRoleRequest { Role = role }));

    // ---------- DeleteUserRequest ----------

    [Fact]
    public void Delete_MissingPassword_FailsOnPassword() =>
        Assert.Contains(nameof(DeleteUserRequest.Password), FailedMembers(new DeleteUserRequest()));

    [Fact]
    public void Delete_ToString_DoesNotLeakPassword() =>
        Assert.DoesNotContain(Password, new DeleteUserRequest { Password = Password }.ToString());
}
