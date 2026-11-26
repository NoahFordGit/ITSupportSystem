using System.Net;
using System.Net.Http.Json;
using ITSupportSystem.API.Tests.Infrastructure;
using ITSupportSystem.Core.Constants;
using ITSupportSystem.Core.DTOs.Users;

namespace ITSupportSystem.API.Tests.Controllers;

public class AccountControllerTests : ApiTestBase
{
    private static RegisterUserRequest NewRegisterRequest(string userName = "newuser") => new()
    {
        UserName = userName,
        Email = $"{userName}@example.com",
        Password = DefaultPassword,
        FirstName = "New",
        LastName = "User",
        PhoneNumber = "423-555-0199"
    };

    // ---------- POST /api/account/register ----------

    [Fact]
    public async Task Register_ValidRequest_Returns201WithUserResponse()
    {
        var request = NewRegisterRequest();
        var before = DateTime.UtcNow;

        var response = await Anonymous.PostAsJsonAsync("/api/account/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Id));
        Assert.Equal(request.UserName, body.UserName);
        Assert.Equal(request.Email, body.Email);
        Assert.Equal(request.FirstName, body.FirstName);
        Assert.Equal(request.LastName, body.LastName);
        Assert.Equal(request.PhoneNumber, body.PhoneNumber);
        Assert.Equal(Roles.User, body.Role);
        Assert.False(body.IsLockedOut);
        Assert.InRange(body.DateCreated, before.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task Register_LocationHeader_PointsAtMe()
    {
        var response = await Anonymous.PostAsJsonAsync("/api/account/register", NewRegisterRequest());

        Assert.NotNull(response.Headers.Location);
        Assert.Equal("/api/account/me", response.Headers.Location!.AbsolutePath, ignoreCase: true);
    }

    [Fact]
    public async Task Register_ResponseNeverContainsPasswordOrHash()
    {
        var response = await Anonymous.PostAsJsonAsync("/api/account/register", NewRegisterRequest());
        var raw = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(DefaultPassword, raw);
        Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_NewUserCanLogInAndIsInUserRole()
    {
        var request = NewRegisterRequest();
        await Anonymous.PostAsJsonAsync("/api/account/register", request);

        using var client = await LoginAsync(request.UserName, request.Password);
        var me = await client.GetFromJsonAsync<UserResponse>("/api/account/me");

        Assert.Equal(Roles.User, me!.Role);
    }

    [Fact]
    public async Task Register_DuplicateUserName_Returns400WithIdentityError()
    {
        await Anonymous.PostAsJsonAsync("/api/account/register", NewRegisterRequest("dupe"));

        var response = await Anonymous.PostAsJsonAsync("/api/account/register", NewRegisterRequest("dupe"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("DuplicateUserName", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Register_PasswordFailsIdentityRules_Returns400WithIdentityErrors()
    {
        // Long enough for the DTO (8+), but no digit/uppercase/symbol for Identity
        var request = NewRegisterRequest() with { Password = "alllowercase" };

        var response = await Anonymous.PostAsJsonAsync("/api/account/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        Assert.Contains("PasswordRequiresDigit", raw);
        Assert.Contains("PasswordRequiresUpper", raw);
        Assert.Contains("PasswordRequiresNonAlphanumeric", raw);
    }

    [Theory]
    [InlineData("Email", "not-an-email")]
    [InlineData("Password", "Sh0rt!")]
    [InlineData("FirstName", "")]
    [InlineData("LastName", "")]
    [InlineData("UserName", "")]
    public async Task Register_InvalidField_Returns400ValidationProblem(string field, string value)
    {
        var request = NewRegisterRequest();
        request = field switch
        {
            "Email" => request with { Email = value },
            "Password" => request with { Password = value },
            "FirstName" => request with { FirstName = value },
            "LastName" => request with { LastName = value },
            "UserName" => request with { UserName = value },
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        };

        var response = await Anonymous.PostAsJsonAsync("/api/account/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>();
        Assert.Contains(problem!.Errors.Keys, k => k.Equals(field, StringComparison.OrdinalIgnoreCase));
    }

    // ---------- GET /api/account/me ----------

    [Fact]
    public async Task GetMe_Anonymous_Returns401()
    {
        var response = await Anonymous.GetAsync("/api/account/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_AsSeededAdmin_ReturnsAdminDetails()
    {
        using var admin = await LoginAsAdminAsync();

        var response = await admin.GetAsync("/api/account/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(AdminUserName, me!.UserName);
        Assert.Equal(AdminEmail, me.Email);
        Assert.Equal("System", me.FirstName);
        Assert.Equal("Administrator", me.LastName);
        Assert.Equal(Roles.Admin, me.Role);
    }

    [Fact]
    public async Task GetMe_AfterAccountDeleted_Returns404()
    {
        // The token is still valid, but the user it points to is gone
        var (client, _) = await CreateAndLoginAsync("ghost", Roles.User);
        await DeleteWithBodyAsync(client, "/api/account/me", new DeleteUserRequest { Password = DefaultPassword });

        var response = await client.GetAsync("/api/account/me");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- PUT /api/account/me ----------

    [Fact]
    public async Task UpdateMe_Anonymous_Returns401()
    {
        var response = await Anonymous.PutAsJsonAsync("/api/account/me", new UpdateUserRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_ValidRequest_Returns204AndPersistsChanges()
    {
        var (client, original) = await CreateAndLoginAsync("editme", Roles.User);
        var update = new UpdateUserRequest
        {
            UserName = "edited",
            Email = "edited@example.com",
            FirstName = "Edited",
            LastName = "Person",
            PhoneNumber = "423-555-0123"
        };

        var response = await client.PutAsJsonAsync("/api/account/me", update);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var me = await client.GetFromJsonAsync<UserResponse>("/api/account/me");
        Assert.Equal(original.Id, me!.Id);
        Assert.Equal(update.UserName, me.UserName);
        Assert.Equal(update.Email, me.Email);
        Assert.Equal(update.FirstName, me.FirstName);
        Assert.Equal(update.LastName, me.LastName);
        Assert.Equal(update.PhoneNumber, me.PhoneNumber);
        Assert.Equal(Roles.User, me.Role); // role untouched

        // The new username is what you log in with now
        Assert.Equal(HttpStatusCode.OK, (await LoginRawAsync("edited", DefaultPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginRawAsync("editme", DefaultPassword)).StatusCode);
    }

    [Fact]
    public async Task UpdateMe_UserNameTakenBySomeoneElse_Returns400()
    {
        var (client, _) = await CreateAndLoginAsync("someone", Roles.User);
        var update = new UpdateUserRequest
        {
            UserName = AdminUserName,
            Email = "someone@example.com",
            FirstName = "Some",
            LastName = "One"
        };

        var response = await client.PutAsJsonAsync("/api/account/me", update);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("DuplicateUserName", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UpdateMe_InvalidEmail_Returns400()
    {
        var (client, _) = await CreateAndLoginAsync("bademail", Roles.User);
        var update = new UpdateUserRequest
        {
            UserName = "bademail",
            Email = "nope",
            FirstName = "A",
            LastName = "B"
        };

        var response = await client.PutAsJsonAsync("/api/account/me", update);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- DELETE /api/account/me ----------

    [Fact]
    public async Task DeleteMe_Anonymous_Returns401()
    {
        var response = await DeleteWithBodyAsync(Anonymous, "/api/account/me",
            new DeleteUserRequest { Password = DefaultPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMe_WrongPassword_Returns400AndKeepsAccount()
    {
        var (client, _) = await CreateAndLoginAsync("keepme", Roles.User);

        var response = await DeleteWithBodyAsync(client, "/api/account/me",
            new DeleteUserRequest { Password = "WrongPass1!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Incorrect password.", await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await LoginRawAsync("keepme", DefaultPassword)).StatusCode);
    }

    [Fact]
    public async Task DeleteMe_MissingPassword_Returns400()
    {
        var (client, _) = await CreateAndLoginAsync("nopass", Roles.User);

        var response = await DeleteWithBodyAsync(client, "/api/account/me", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMe_CorrectPassword_Returns204AndRemovesAccount()
    {
        var (client, user) = await CreateAndLoginAsync("deleteme", Roles.User);

        var response = await DeleteWithBodyAsync(client, "/api/account/me",
            new DeleteUserRequest { Password = DefaultPassword });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginRawAsync("deleteme", DefaultPassword)).StatusCode);

        using var admin = await LoginAsAdminAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/users/{user.Id}")).StatusCode);
    }

    [Fact]
    public async Task DeleteMe_LastAdmin_Returns400AndKeepsAccount()
    {
        using var admin = await LoginAsAdminAsync();

        var response = await DeleteWithBodyAsync(admin, "/api/account/me",
            new DeleteUserRequest { Password = AdminPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Cannot remove the last Admin.", await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/account/me")).StatusCode);
    }

    [Fact]
    public async Task DeleteMe_AdminWhenAnotherAdminExists_Returns204()
    {
        var (secondAdmin, _) = await CreateAndLoginAsync("admin2", Roles.Admin);

        var response = await DeleteWithBodyAsync(secondAdmin, "/api/account/me",
            new DeleteUserRequest { Password = DefaultPassword });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private sealed record ValidationProblem(Dictionary<string, string[]> Errors);
}
