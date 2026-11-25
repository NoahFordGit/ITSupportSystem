using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ITSupportSystem.Core.DTOs.Users;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ITSupportSystem.API.Tests.Infrastructure;

/// <summary>
/// Base class for HTTP-level tests. xUnit creates a new instance of the test
/// class for every test, so every test gets a fresh app and a fresh database
/// containing only the seeded roles and admin.
/// </summary>
public abstract class ApiTestBase : IDisposable
{
    // The seeded admin's login, supplied to the app by ApiFactory
    protected const string AdminUserName = ApiFactory.AdminUserName;
    protected const string AdminEmail = ApiFactory.AdminEmail;
    protected const string AdminPassword = ApiFactory.AdminPassword;

    // Satisfies Identity's default password rules and the DTO's MinLength(8)
    protected const string DefaultPassword = "Passw0rd!";

    protected ApiFactory Factory { get; } = new();

    /// <summary>A client with no credentials.</summary>
    protected HttpClient Anonymous { get; }

    protected ApiTestBase()
    {
        Anonymous = NewClient();
    }

    protected HttpClient NewClient() =>
        Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

    /// <summary>
    /// Logs in through the Identity API endpoint. Note: /login's "email" field
    /// is actually matched against UserName by PasswordSignInAsync.
    /// </summary>
    protected async Task<HttpResponseMessage> LoginRawAsync(string userName, string password) =>
        await Anonymous.PostAsJsonAsync("/api/auth/login", new { email = userName, password });

    protected async Task<HttpClient> LoginAsync(string userName, string password)
    {
        var response = await LoginRawAsync(userName, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.False(string.IsNullOrEmpty(token?.AccessToken));

        var client = NewClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return client;
    }

    protected Task<HttpClient> LoginAsAdminAsync() => LoginAsync(AdminUserName, AdminPassword);

    protected static CreateUserRequest NewCreateRequest(string userName, string role) => new()
    {
        UserName = userName,
        Email = $"{userName}@example.com",
        Password = DefaultPassword,
        FirstName = $"{userName}-first",
        LastName = $"{userName}-last",
        PhoneNumber = "423-555-0100",
        Role = role
    };

    /// <summary>Creates a user through the admin-only POST /api/users endpoint.</summary>
    protected async Task<UserResponse> CreateUserAsync(string userName, string role)
    {
        using var admin = await LoginAsAdminAsync();
        var response = await admin.PostAsJsonAsync("/api/users", NewCreateRequest(userName, role));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(body);
        return body!;
    }

    /// <summary>Creates a user in the given role and returns a client logged in as them.</summary>
    protected async Task<(HttpClient Client, UserResponse User)> CreateAndLoginAsync(string userName, string role)
    {
        var user = await CreateUserAsync(userName, role);
        var client = await LoginAsync(userName, DefaultPassword);
        return (client, user);
    }

    protected async Task<UserResponse> GetSeededAdminAsync()
    {
        using var admin = await LoginAsAdminAsync();
        var me = await admin.GetFromJsonAsync<UserResponse>("/api/account/me");
        Assert.NotNull(me);
        return me!;
    }

    /// <summary>HttpClient.DeleteAsync can't send a body, so build the request by hand.</summary>
    protected static Task<HttpResponseMessage> DeleteWithBodyAsync<T>(HttpClient client, string url, T body) =>
        client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, url)
        {
            Content = JsonContent.Create(body)
        });

    public void Dispose()
    {
        Anonymous.Dispose();
        Factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed record TokenResponse(string TokenType, string AccessToken, long ExpiresIn, string RefreshToken);
}
