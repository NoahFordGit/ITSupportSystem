using System.Net;
using System.Net.Http.Json;
using ITSupportSystem.API.Tests.Infrastructure;

namespace ITSupportSystem.API.Tests.Controllers;

/// <summary>
/// Tests for the built-in Identity endpoints mapped under /api/auth in Program.cs.
/// </summary>
public class AuthEndpointTests : ApiTestBase
{
    [Theory]
    [InlineData("/api/auth/register")]
    [InlineData("/api/auth/REGISTER")]
    [InlineData("/api/auth/register/")]
    public async Task BuiltInRegister_IsBlocked_Returns404(string url)
    {
        var response = await Anonymous.PostAsJsonAsync(url, new
        {
            email = "sneaky@example.com",
            password = DefaultPassword
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // And the user really was not created
        var login = await LoginRawAsync("sneaky@example.com", DefaultPassword);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Login_WithSeededAdmin_ReturnsBearerToken()
    {
        var response = await LoginRawAsync(AdminUserName, AdminPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(body);
        Assert.Equal("Bearer", body!["tokenType"].ToString());
        Assert.False(string.IsNullOrWhiteSpace(body["accessToken"].ToString()));
        Assert.False(string.IsNullOrWhiteSpace(body["refreshToken"].ToString()));
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var response = await LoginRawAsync(AdminUserName, "WrongPassword1!");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownUser_Returns401()
    {
        var response = await LoginRawAsync("nobody", DefaultPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
