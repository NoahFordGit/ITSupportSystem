using System.Net;
using System.Net.Http.Json;
using ITSupportSystem.API.Tests.Infrastructure;
using ITSupportSystem.Core.Constants;
using ITSupportSystem.Core.DTOs.Users;

namespace ITSupportSystem.API.Tests.Controllers;

public class UsersControllerTests : ApiTestBase
{
    private static UpdateUserRequest NewUpdateRequest(string userName) => new()
    {
        UserName = userName,
        Email = $"{userName}@example.com",
        FirstName = "Updated",
        LastName = "Name",
        PhoneNumber = "423-555-0111"
    };

    // ---------- Authorization matrix ----------
    // Who gets in where, without caring about the response body.

    public static TheoryData<string, string> AdminOnlyEndpoints => new()
    {
        { "POST", "/api/users" },
        { "PUT", "/api/users/some-id" },
        { "DELETE", "/api/users/some-id" },
        { "PUT", "/api/users/some-id/role" },
    };

    public static TheoryData<string, string> StaffOrAdminEndpoints => new()
    {
        { "GET", "/api/users" },
        { "GET", "/api/users/some-id" },
    };

    private static HttpRequestMessage BuildRequest(string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST") request.Content = JsonContent.Create(NewCreateRequest("authz", Roles.User));
        if (method is "PUT" && url.EndsWith("/role")) request.Content = JsonContent.Create(new ChangeRoleRequest { Role = Roles.Staff });
        else if (method is "PUT") request.Content = JsonContent.Create(NewUpdateRequest("authz"));
        return request;
    }

    [Theory]
    [MemberData(nameof(AdminOnlyEndpoints))]
    [MemberData(nameof(StaffOrAdminEndpoints))]
    [InlineData("GET", "/api/users/roles")]
    public async Task AnyProtectedEndpoint_Anonymous_Returns401(string method, string url)
    {
        var response = await Anonymous.SendAsync(BuildRequest(method, url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AdminOnlyEndpoints))]
    [MemberData(nameof(StaffOrAdminEndpoints))]
    public async Task StaffOrAdminEndpoints_AsUser_Returns403(string method, string url)
    {
        var (client, _) = await CreateAndLoginAsync("plainuser", Roles.User);

        var response = await client.SendAsync(BuildRequest(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AdminOnlyEndpoints))]
    public async Task AdminOnlyEndpoints_AsStaff_Returns403(string method, string url)
    {
        var (client, _) = await CreateAndLoginAsync("staffer", Roles.Staff);

        var response = await client.SendAsync(BuildRequest(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- GET /api/users ----------

    [Fact]
    public async Task GetAll_AsAdmin_ReturnsEveryUserWithTheirRole()
    {
        await CreateUserAsync("staff1", Roles.Staff);
        await CreateUserAsync("user1", Roles.User);
        using var admin = await LoginAsAdminAsync();

        var response = await admin.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<UserResponse>>();
        Assert.Equal(3, users!.Count);
        Assert.Equal(Roles.Admin, users.Single(u => u.UserName == AdminUserName).Role);
        Assert.Equal(Roles.Staff, users.Single(u => u.UserName == "staff1").Role);
        Assert.Equal(Roles.User, users.Single(u => u.UserName == "user1").Role);
    }

    [Fact]
    public async Task GetAll_AsStaff_Returns200()
    {
        var (client, _) = await CreateAndLoginAsync("staffer", Roles.Staff);

        var users = await client.GetFromJsonAsync<List<UserResponse>>("/api/users");

        Assert.Equal(2, users!.Count);
    }

    [Fact]
    public async Task GetAll_ResponseNeverContainsSecrets()
    {
        await CreateUserAsync("user1", Roles.User);
        using var admin = await LoginAsAdminAsync();

        var raw = await admin.GetStringAsync("/api/users");

        Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("concurrencyStamp", raw, StringComparison.OrdinalIgnoreCase);
    }

    // ---------- GET /api/users/{id} ----------

    [Fact]
    public async Task GetById_Existing_ReturnsUser()
    {
        var created = await CreateUserAsync("findme", Roles.Staff);
        using var admin = await LoginAsAdminAsync();

        var response = await admin.GetAsync($"/api/users/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(created, user);
    }

    [Fact]
    public async Task GetById_Unknown_Returns404()
    {
        using var admin = await LoginAsAdminAsync();

        var response = await admin.GetAsync("/api/users/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- POST /api/users ----------

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Staff)]
    [InlineData(Roles.User)]
    public async Task Create_AsAdmin_Returns201WithUserInRequestedRole(string role)
    {
        using var admin = await LoginAsAdminAsync();
        var request = NewCreateRequest("created", role);

        var response = await admin.PostAsJsonAsync("/api/users", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Id));
        Assert.Equal(request.UserName, body.UserName);
        Assert.Equal(request.Email, body.Email);
        Assert.Equal(request.FirstName, body.FirstName);
        Assert.Equal(request.LastName, body.LastName);
        Assert.Equal(request.PhoneNumber, body.PhoneNumber);
        Assert.Equal(role, body.Role);
        Assert.False(body.IsLockedOut);

        // And the stored role matches what was returned
        var fetched = await admin.GetFromJsonAsync<UserResponse>($"/api/users/{body.Id}");
        Assert.Equal(role, fetched!.Role);
    }

    [Fact]
    public async Task Create_LocationHeader_PointsAtGetById()
    {
        using var admin = await LoginAsAdminAsync();

        var response = await admin.PostAsJsonAsync("/api/users", NewCreateRequest("located", Roles.User));
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();

        Assert.NotNull(response.Headers.Location);
        Assert.Equal($"/api/users/{body!.Id}", response.Headers.Location!.AbsolutePath, ignoreCase: true);

        // Following the Location header returns the same user
        var followed = await admin.GetFromJsonAsync<UserResponse>(response.Headers.Location);
        Assert.Equal(body, followed);
    }

    [Fact]
    public async Task Create_CreatedUserCanLogIn()
    {
        await CreateUserAsync("canlogin", Roles.User);

        var response = await LoginRawAsync("canlogin", DefaultPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidRole_Returns400AndCreatesNothing()
    {
        using var admin = await LoginAsAdminAsync();
        var request = NewCreateRequest("badrole", Roles.User) with { Role = "SuperAdmin" };

        var response = await admin.PostAsJsonAsync("/api/users", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Role", await response.Content.ReadAsStringAsync());
        var users = await admin.GetFromJsonAsync<List<UserResponse>>("/api/users");
        Assert.DoesNotContain(users!, u => u.UserName == "badrole");
    }

    [Fact]
    public async Task Create_DuplicateUserName_Returns400()
    {
        using var admin = await LoginAsAdminAsync();

        var response = await admin.PostAsJsonAsync("/api/users", NewCreateRequest(AdminUserName, Roles.User));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("DuplicateUserName", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Create_WeakPassword_Returns400()
    {
        using var admin = await LoginAsAdminAsync();
        var request = NewCreateRequest("weak", Roles.User) with { Password = "password" };

        var response = await admin.PostAsJsonAsync("/api/users", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("PasswordRequiresDigit", await response.Content.ReadAsStringAsync());
    }

    // ---------- PUT /api/users/{id} ----------

    [Fact]
    public async Task Update_Existing_Returns204AndPersistsChanges()
    {
        var created = await CreateUserAsync("before", Roles.Staff);
        using var admin = await LoginAsAdminAsync();
        var update = NewUpdateRequest("after");

        var response = await admin.PutAsJsonAsync($"/api/users/{created.Id}", update);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var fetched = await admin.GetFromJsonAsync<UserResponse>($"/api/users/{created.Id}");
        Assert.Equal(update.UserName, fetched!.UserName);
        Assert.Equal(update.Email, fetched.Email);
        Assert.Equal(update.FirstName, fetched.FirstName);
        Assert.Equal(update.LastName, fetched.LastName);
        Assert.Equal(update.PhoneNumber, fetched.PhoneNumber);
        Assert.Equal(Roles.Staff, fetched.Role);           // unchanged
        Assert.Equal(created.DateCreated, fetched.DateCreated); // unchanged
    }

    [Fact]
    public async Task Update_Unknown_Returns404()
    {
        using var admin = await LoginAsAdminAsync();

        var response = await admin.PutAsJsonAsync("/api/users/does-not-exist", NewUpdateRequest("x"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_UserNameTaken_Returns400()
    {
        var created = await CreateUserAsync("taken", Roles.User);
        using var admin = await LoginAsAdminAsync();

        var response = await admin.PutAsJsonAsync($"/api/users/{created.Id}", NewUpdateRequest(AdminUserName));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("DuplicateUserName", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Update_MissingRequiredField_Returns400()
    {
        var created = await CreateUserAsync("required", Roles.User);
        using var admin = await LoginAsAdminAsync();

        var response = await admin.PutAsJsonAsync($"/api/users/{created.Id}",
            NewUpdateRequest("required") with { FirstName = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("FirstName", await response.Content.ReadAsStringAsync());
    }

    // ---------- DELETE /api/users/{id} ----------

    [Fact]
    public async Task Delete_Existing_Returns204AndRemovesUser()
    {
        var created = await CreateUserAsync("doomed", Roles.User);
        using var admin = await LoginAsAdminAsync();

        var response = await admin.DeleteAsync($"/api/users/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/users/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginRawAsync("doomed", DefaultPassword)).StatusCode);
    }

    [Fact]
    public async Task Delete_Unknown_Returns404()
    {
        using var admin = await LoginAsAdminAsync();

        var response = await admin.DeleteAsync("/api/users/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_LastAdmin_Returns400AndKeepsAdmin()
    {
        var seeded = await GetSeededAdminAsync();
        using var admin = await LoginAsAdminAsync();

        var response = await admin.DeleteAsync($"/api/users/{seeded.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Cannot remove the last Admin.", await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/users/{seeded.Id}")).StatusCode);
    }

    [Fact]
    public async Task Delete_AdminWhenAnotherAdminExists_Returns204()
    {
        var secondAdmin = await CreateUserAsync("admin2", Roles.Admin);
        using var admin = await LoginAsAdminAsync();

        var response = await admin.DeleteAsync($"/api/users/{secondAdmin.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ---------- GET /api/users/roles ----------

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Staff)]
    [InlineData(Roles.User)]
    public async Task GetRoles_AnyAuthenticatedUser_ReturnsAllRoles(string role)
    {
        HttpClient client;
        if (role == Roles.Admin) client = await LoginAsAdminAsync();
        else (client, _) = await CreateAndLoginAsync("roleviewer", role);

        var response = await client.GetAsync("/api/users/roles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var roles = await response.Content.ReadFromJsonAsync<List<string>>();
        Assert.Equal(Roles.All.OrderBy(r => r), roles!.OrderBy(r => r));
        client.Dispose();
    }

    // ---------- PUT /api/users/{id}/role ----------

    private static Task<HttpResponseMessage> ChangeRoleAsync(HttpClient client, string id, string role) =>
        client.PutAsJsonAsync($"/api/users/{id}/role", new ChangeRoleRequest { Role = role });

    [Theory]
    [InlineData(Roles.User, Roles.Staff)]
    [InlineData(Roles.User, Roles.Admin)]
    [InlineData(Roles.Staff, Roles.User)]
    [InlineData(Roles.Admin, Roles.User)] // allowed because the seeded admin remains
    public async Task UpdateRole_ChangesRole_Returns204AndReplacesOldRole(string from, string to)
    {
        var created = await CreateUserAsync("rolechange", from);
        using var admin = await LoginAsAdminAsync();

        var response = await ChangeRoleAsync(admin, created.Id, to);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var fetched = await admin.GetFromJsonAsync<UserResponse>($"/api/users/{created.Id}");
        Assert.Equal(to, fetched!.Role);

        // Exactly one role afterwards: the new user's view of /me agrees
        using var self = await LoginAsync("rolechange", DefaultPassword);
        var me = await self.GetFromJsonAsync<UserResponse>("/api/account/me");
        Assert.Equal(to, me!.Role);
    }

    [Fact]
    public async Task UpdateRole_ToStaff_GrantsStaffAccess()
    {
        var created = await CreateUserAsync("promoted", Roles.User);
        using var admin = await LoginAsAdminAsync();
        await ChangeRoleAsync(admin, created.Id, Roles.Staff);

        // Roles are baked into the token at login, so log in again after the change
        using var promoted = await LoginAsync("promoted", DefaultPassword);
        var response = await promoted.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRole_SameRole_Returns204AndChangesNothing()
    {
        var created = await CreateUserAsync("samerole", Roles.Staff);
        using var admin = await LoginAsAdminAsync();

        var response = await ChangeRoleAsync(admin, created.Id, Roles.Staff);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var fetched = await admin.GetFromJsonAsync<UserResponse>($"/api/users/{created.Id}");
        Assert.Equal(Roles.Staff, fetched!.Role);
    }

    [Fact]
    public async Task UpdateRole_DemoteLastAdmin_Returns400AndKeepsAdmin()
    {
        var seeded = await GetSeededAdminAsync();
        using var admin = await LoginAsAdminAsync();

        var response = await ChangeRoleAsync(admin, seeded.Id, Roles.User);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Cannot remove the last Admin.", await response.Content.ReadAsStringAsync());
        var fetched = await admin.GetFromJsonAsync<UserResponse>($"/api/users/{seeded.Id}");
        Assert.Equal(Roles.Admin, fetched!.Role);
    }

    [Fact]
    public async Task UpdateRole_LastAdminToAdmin_Returns204()
    {
        var seeded = await GetSeededAdminAsync();
        using var admin = await LoginAsAdminAsync();

        var response = await ChangeRoleAsync(admin, seeded.Id, Roles.Admin);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRole_InvalidRole_Returns400()
    {
        var created = await CreateUserAsync("invalidrole", Roles.User);
        using var admin = await LoginAsAdminAsync();

        var response = await ChangeRoleAsync(admin, created.Id, "SuperAdmin");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var fetched = await admin.GetFromJsonAsync<UserResponse>($"/api/users/{created.Id}");
        Assert.Equal(Roles.User, fetched!.Role);
    }

    [Fact]
    public async Task UpdateRole_Unknown_Returns404()
    {
        using var admin = await LoginAsAdminAsync();

        var response = await ChangeRoleAsync(admin, "does-not-exist", Roles.Staff);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
