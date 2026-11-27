using System.Net;
using System.Security.Claims;
using System.Text.Json;
using ITSupportSystem.Web.Tests.Infrastructure;

namespace ITSupportSystem.Web.Tests.Controllers;

/// <summary>POST /Account/Login and POST /Account/Logout.</summary>
public class LoginTests : WebTestBase
{
    // ---------- Success ----------

    [Fact]
    public async Task Login_Valid_RedirectsToTickets()
    {
        ApiAcceptsLogin();

        var response = await PostFormAsync("/Account/Login", LoginForm());

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Ticket", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Login_Valid_SendsCredentialsToApi()
    {
        ApiAcceptsLogin();

        await PostFormAsync("/Account/Login", LoginForm("jane@example.com", "Secret123!"));

        var login = Assert.Single(Api.Requests, r => r.Path == "/api/auth/login");
        Assert.Equal(HttpMethod.Post, login.Method);

        using var body = JsonDocument.Parse(login.Body!);
        Assert.Equal("jane@example.com", body.RootElement.GetProperty("email").GetString());
        Assert.Equal("Secret123!", body.RootElement.GetProperty("password").GetString());
    }

    [Fact]
    public async Task Login_Valid_AsksApiForUserWithBearerToken()
    {
        ApiAcceptsLogin();

        await PostFormAsync("/Account/Login", LoginForm());

        var me = Assert.Single(Api.Requests, r => r.Path == "/api/account/me");
        Assert.Equal(HttpMethod.Get, me.Method);
        Assert.Equal($"Bearer {FakeAccessToken}", me.Authorization);
    }

    [Fact]
    public async Task Login_Valid_SetsAuthCookie()
    {
        ApiAcceptsLogin();

        var response = await PostFormAsync("/Account/Login", LoginForm());

        var cookie = GetSetCookie(response, AuthCookieName);
        Assert.NotNull(cookie);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_Valid_CookieHoldsUserClaims()
    {
        var user = SampleUser("brock@example.com") with
        {
            Id = "abc-999",
            FirstName = "Brock",
            LastName = "Morell",
            Role = "Staff"
        };
        ApiAcceptsLogin(user);

        var response = await PostFormAsync("/Account/Login", LoginForm(user.Email));

        var ticket = ReadAuthTicket(response);
        Assert.Equal("abc-999", Claim(ticket, ClaimTypes.NameIdentifier));
        Assert.Equal("Brock Morell", Claim(ticket, ClaimTypes.Name));
        Assert.Equal("brock@example.com", Claim(ticket, ClaimTypes.Email));
        Assert.Equal("Staff", Claim(ticket, ClaimTypes.Role));
        Assert.Equal(FakeAccessToken, Claim(ticket, "access_token"));
    }

    [Fact]
    public async Task Login_Valid_CookieExpiresWhenApiTokenDoes()
    {
        ApiAcceptsLogin(expiresIn: 600);
        var before = DateTimeOffset.UtcNow;

        var response = await PostFormAsync("/Account/Login", LoginForm());

        var expires = ReadAuthTicket(response).Properties.ExpiresUtc;
        Assert.NotNull(expires);
        Assert.InRange(expires!.Value, before.AddSeconds(590), DateTimeOffset.UtcNow.AddSeconds(610));
    }

    [Fact]
    public async Task Login_WithoutRememberMe_IsSessionCookie()
    {
        ApiAcceptsLogin();

        var response = await PostFormAsync("/Account/Login", LoginForm(rememberMe: false));

        Assert.False(ReadAuthTicket(response).Properties.IsPersistent);
        Assert.DoesNotContain("expires=", GetSetCookie(response, AuthCookieName)!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WithRememberMe_IsPersistentCookie()
    {
        ApiAcceptsLogin();

        var response = await PostFormAsync("/Account/Login", LoginForm(rememberMe: true));

        Assert.True(ReadAuthTicket(response).Properties.IsPersistent);
        Assert.Contains("expires=", GetSetCookie(response, AuthCookieName)!, StringComparison.OrdinalIgnoreCase);
    }

    // ---------- Form validation (API never called) ----------

    [Fact]
    public async Task Login_EmptyForm_ShowsFieldErrors_AndSkipsApi()
    {
        var response = await PostFormAsync("/Account/Login", LoginForm(email: "", password: ""));
        var html = await ReadHtmlAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("The Email field is required.", html);
        Assert.Contains("The Password field is required.", html);
        Assert.Empty(Api.Requests);
    }

    [Fact]
    public async Task Login_BadEmail_ShowsFieldError_AndSkipsApi()
    {
        var response = await PostFormAsync("/Account/Login", LoginForm(email: "not-an-email"));
        var html = await ReadHtmlAsync(response);

        Assert.Contains("The Email field is not a valid e-mail address.", html);
        Assert.Empty(Api.Requests);
    }

    [Fact]
    public async Task Login_Invalid_KeepsTypedEmail_ButNotPassword()
    {
        var response = await PostFormAsync("/Account/Login", LoginForm(email: "typed@", password: "typed-password"));
        var html = await ReadHtmlAsync(response);

        Assert.Contains("value=\"typed@\"", html);
        Assert.DoesNotContain("typed-password", html);
    }

    [Fact]
    public async Task Login_Invalid_StaysOnLoginTab()
    {
        var html = await ReadHtmlAsync(await PostFormAsync("/Account/Login", LoginForm(email: "")));

        Assert.False(IsHidden(FormTag(html, "login-form")));
        Assert.True(IsHidden(FormTag(html, "register-form")));
    }

    // ---------- API failures ----------

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Login_ApiRejects_ShowsInvalidCredentials(HttpStatusCode status)
    {
        Api.OnJson(HttpMethod.Post, "/api/auth/login", status);

        var response = await PostFormAsync("/Account/Login", LoginForm());
        var html = await ReadHtmlAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invalid email or password.", html);
        Assert.Null(GetSetCookie(response, AuthCookieName));
        Assert.DoesNotContain(Api.Requests, r => r.Path == "/api/account/me");
    }

    [Fact]
    public async Task Login_ApiOffline_ShowsCantReachServer()
    {
        Api.GoOffline();

        var response = await PostFormAsync("/Account/Login", LoginForm());
        var html = await ReadHtmlAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Can't reach the server. Please try again later.", html);
        Assert.Null(GetSetCookie(response, AuthCookieName));
    }

    [Fact]
    public async Task Login_ApiReturnsNullToken_ShowsLoginFailed()
    {
        Api.On(HttpMethod.Post, "/api/auth/login", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
        });

        var html = await ReadHtmlAsync(await PostFormAsync("/Account/Login", LoginForm()));

        Assert.Contains("Login failed. Please try again.", html);
    }

    [Fact]
    public async Task Login_MeEndpointFails_ShowsLoginFailed_AndNoCookie()
    {
        ApiAcceptsLogin();
        Api.OnJson(HttpMethod.Get, "/api/account/me", HttpStatusCode.Unauthorized);

        var response = await PostFormAsync("/Account/Login", LoginForm());
        var html = await ReadHtmlAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Login failed. Please try again.", html);
        Assert.Null(GetSetCookie(response, AuthCookieName));
    }

    [Fact]
    public async Task Login_ApiError_KeepsTypedEmail()
    {
        Api.OnJson(HttpMethod.Post, "/api/auth/login", HttpStatusCode.Unauthorized);

        var html = await ReadHtmlAsync(await PostFormAsync("/Account/Login", LoginForm(email: "kept@example.com")));

        Assert.Contains("value=\"kept@example.com\"", html);
    }

    // ---------- Antiforgery ----------

    [Fact]
    public async Task Login_WithoutAntiforgeryToken_Returns400()
    {
        ApiAcceptsLogin();

        var response = await Client.PostAsync("/Account/Login", new FormUrlEncodedContent(LoginForm()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(Api.Requests);
    }

    // ---------- Logout ----------

    [Fact]
    public async Task Logout_RedirectsToLogin_AndClearsCookie()
    {
        await LogInAsync();

        var response = await PostFormAsync("/Account/Logout", []);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location?.OriginalString);

        // Signing out sends the cookie back empty with an expiry date in the past
        var cookie = GetSetCookie(response, AuthCookieName);
        Assert.NotNull(cookie);
        Assert.StartsWith($"{AuthCookieName}=;", cookie);
        Assert.Contains("expires=Thu, 01 Jan 1970", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Logout_WithoutAntiforgeryToken_Returns400()
    {
        await LogInAsync();

        var response = await Client.PostAsync("/Account/Logout", new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ViaGet_IsNotAllowed()
    {
        var response = await Client.GetAsync("/Account/Logout");

        // Only [HttpPost] exists, so a link or <img> can't log someone out
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
