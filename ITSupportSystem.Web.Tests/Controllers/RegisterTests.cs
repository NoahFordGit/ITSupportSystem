using System.Net;
using System.Text.Json;
using ITSupportSystem.Web.Tests.Infrastructure;

namespace ITSupportSystem.Web.Tests.Controllers;

/// <summary>POST /Account/Register.</summary>
public class RegisterTests : WebTestBase
{
    private const string RegisterPath = "/api/account/register";

    private void ApiReturnsIdentityErrors(params (string Code, string Description)[] errors) =>
        Api.OnJson(HttpMethod.Post, RegisterPath, HttpStatusCode.BadRequest,
            errors.Select(e => new { code = e.Code, description = e.Description }));

    // ---------- Success ----------

    [Fact]
    public async Task Register_Valid_RedirectsToLogin()
    {
        Api.OnJson(HttpMethod.Post, RegisterPath, HttpStatusCode.Created, SampleUser());

        var response = await PostFormAsync("/Account/Register", RegisterForm());

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Register_Valid_SendsFormToApi_WithEmailAsUserName()
    {
        Api.OnJson(HttpMethod.Post, RegisterPath, HttpStatusCode.Created, SampleUser());

        await PostFormAsync("/Account/Register",
            RegisterForm("Brock", "Morell", "brock@example.com", "Secret123!"));

        var request = Assert.Single(Api.Requests, r => r.Path == RegisterPath);
        Assert.Equal(HttpMethod.Post, request.Method);

        using var body = JsonDocument.Parse(request.Body!);
        var json = body.RootElement;
        Assert.Equal("Brock", json.GetProperty("firstName").GetString());
        Assert.Equal("Morell", json.GetProperty("lastName").GetString());
        Assert.Equal("brock@example.com", json.GetProperty("email").GetString());
        Assert.Equal("Secret123!", json.GetProperty("password").GetString());

        // The seeded-admin fix relies on this: people log in by email, so
        // the username has to be the email too.
        Assert.Equal("brock@example.com", json.GetProperty("userName").GetString());
    }

    [Fact]
    public async Task Register_Valid_DoesNotLogTheUserIn()
    {
        Api.OnJson(HttpMethod.Post, RegisterPath, HttpStatusCode.Created, SampleUser());

        var response = await PostFormAsync("/Account/Register", RegisterForm());

        Assert.Null(GetSetCookie(response, AuthCookieName));
    }

    // ---------- Form validation (API never called) ----------

    [Fact]
    public async Task Register_EmptyForm_ShowsFieldErrors_AndSkipsApi()
    {
        var response = await PostFormAsync("/Account/Register", RegisterForm("", "", "", ""));
        var html = await ReadHtmlAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("The First name field is required.", html);
        Assert.Contains("The Last Name field is required.", html);
        Assert.Contains("The Email field is required.", html);
        Assert.Contains("The Password field is required.", html);
        Assert.Empty(Api.Requests);
    }

    [Fact]
    public async Task Register_ShortPassword_ShowsFieldError_AndSkipsApi()
    {
        var html = await ReadHtmlAsync(await PostFormAsync("/Account/Register", RegisterForm(password: "short")));

        Assert.Contains("The field Password must be a string or array type with a minimum length of '8'.", html);
        Assert.Empty(Api.Requests);
    }

    [Fact]
    public async Task Register_BadEmail_ShowsFieldError_AndSkipsApi()
    {
        var html = await ReadHtmlAsync(await PostFormAsync("/Account/Register", RegisterForm(email: "nope")));

        Assert.Contains("The Email field is not a valid e-mail address.", html);
        Assert.Empty(Api.Requests);
    }

    [Fact]
    public async Task Register_Invalid_ShowsRegisterTab()
    {
        var html = await ReadHtmlAsync(await PostFormAsync("/Account/Register", RegisterForm(firstName: "")));

        Assert.True(IsHidden(FormTag(html, "login-form")));
        Assert.False(IsHidden(FormTag(html, "register-form")));
        Assert.Contains("class=\"tab active\" data-target=\"register-form\"", html);
    }

    [Fact]
    public async Task Register_Invalid_KeepsTypedValues_ButNotPassword()
    {
        var html = await ReadHtmlAsync(await PostFormAsync("/Account/Register",
            RegisterForm("Typed", "Name", "typed@", "typed-password")));

        Assert.Contains("value=\"Typed\"", html);
        Assert.Contains("value=\"Name\"", html);
        Assert.Contains("value=\"typed@\"", html);
        Assert.DoesNotContain("typed-password", html);
    }

    // ---------- API failures ----------

    [Fact]
    public async Task Register_DuplicateEmail_ShowsFriendlyMessage()
    {
        ApiReturnsIdentityErrors(("DuplicateUserName", "Username 'jane@example.com' is already taken."));

        var response = await PostFormAsync("/Account/Register", RegisterForm());
        var html = await ReadHtmlAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("An account with this email already exists", html);
        Assert.DoesNotContain("is already taken", html);
    }

    [Fact]
    public async Task Register_OtherIdentityErrors_ShowsEachDescription()
    {
        ApiReturnsIdentityErrors(
            ("PasswordRequiresDigit", "Passwords must have at least one digit ('0'-'9')."),
            ("PasswordRequiresUpper", "Passwords must have at least one uppercase ('A'-'Z')."));

        var html = await ReadHtmlAsync(await PostFormAsync("/Account/Register", RegisterForm()));

        Assert.Contains("Passwords must have at least one digit ('0'-'9').", html);
        Assert.Contains("Passwords must have at least one uppercase ('A'-'Z').", html);
    }

    [Fact]
    public async Task Register_ApiErrorNotJson_ShowsGenericMessage()
    {
        Api.On(HttpMethod.Post, RegisterPath, _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Something broke", System.Text.Encoding.UTF8, "text/plain")
        });

        var html = await ReadHtmlAsync(await PostFormAsync("/Account/Register", RegisterForm()));

        Assert.Contains("Registration Failed. Please check your details", html);
    }

    [Fact]
    public async Task Register_ApiOffline_ShowsCantReachServer()
    {
        Api.GoOffline();

        var response = await PostFormAsync("/Account/Register", RegisterForm());
        var html = await ReadHtmlAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Can't reach server. Please try again later.", html);
    }

    [Fact]
    public async Task Register_ApiError_ShowsRegisterTab()
    {
        ApiReturnsIdentityErrors(("DuplicateUserName", "taken"));

        var html = await ReadHtmlAsync(await PostFormAsync("/Account/Register", RegisterForm()));

        Assert.True(IsHidden(FormTag(html, "login-form")));
        Assert.False(IsHidden(FormTag(html, "register-form")));
    }

    // ---------- Antiforgery ----------

    [Fact]
    public async Task Register_WithoutAntiforgeryToken_Returns400()
    {
        var response = await Client.PostAsync("/Account/Register", new FormUrlEncodedContent(RegisterForm()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(Api.Requests);
    }
}
