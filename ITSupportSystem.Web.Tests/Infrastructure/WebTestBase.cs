using System.Net;
using System.Net.Http.Json;

using System.Text.RegularExpressions;
using ITSupportSystem.Core.DTOs.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ITSupportSystem.Web.Tests.Infrastructure;

/// <summary>
/// Base class for Web tests. xUnit creates a new instance of the test class for
/// every test, so every test gets a fresh app, a fresh FakeApi and a fresh
/// browser-like client (its own cookie jar).
/// </summary>
public abstract partial class WebTestBase : IDisposable
{
    protected const string AuthCookieName = ".AspNetCore.Cookies";
    protected const string FakeAccessToken = "fake-access-token";

    protected WebFactory Factory { get; } = new();
    protected FakeApi Api => Factory.Api;

    /// <summary>
    /// Acts like a browser: keeps cookies between requests, but doesn't follow
    /// redirects so tests can check where a redirect points.
    /// </summary>
    protected HttpClient Client { get; }

    protected WebTestBase()
    {
        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    // ---------- Forms ----------

    /// <summary>
    /// Every POST action has [ValidateAntiForgeryToken], so a form post needs the
    /// hidden token from the page plus the matching cookie, like a real browser.
    /// Getting the page each time also means the token matches whoever is
    /// currently signed in.
    /// </summary>
    protected async Task<string> GetAntiforgeryTokenAsync()
    {
        var html = await Client.GetStringAsync("/Account/Login");
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success, "The login page has no antiforgery token.");
        return match.Groups[1].Value;
    }

    protected async Task<HttpResponseMessage> PostFormAsync(string url, Dictionary<string, string> fields)
    {
        var withToken = new Dictionary<string, string>(fields)
        {
            ["__RequestVerificationToken"] = await GetAntiforgeryTokenAsync()
        };
        return await Client.PostAsync(url, new FormUrlEncodedContent(withToken));
    }

    protected static Dictionary<string, string> LoginForm(
        string email = "jane@example.com", string password = "Passw0rd!", bool rememberMe = false)
    {
        var form = new Dictionary<string, string>
        {
            ["Login.Email"] = email,
            ["Login.Password"] = password
        };
        if (rememberMe)
        {
            form["Login.RememberMe"] = "true";
        }
        return form;
    }

    protected static Dictionary<string, string> RegisterForm(
        string firstName = "Jane", string lastName = "Doe",
        string email = "jane@example.com", string password = "Passw0rd!") => new()
    {
        ["Register.FirstName"] = firstName,
        ["Register.LastName"] = lastName,
        ["Register.Email"] = email,
        ["Register.Password"] = password
    };

    // ---------- Fake API set-ups ----------

    protected static UserResponse SampleUser(string email = "jane@example.com") => new()
    {
        Id = "user-123",
        UserName = email,
        Email = email,
        FirstName = "Jane",
        LastName = "Doe",
        Role = "User"
    };

    /// <summary>The API accepts the login and returns the given user from /me.</summary>
    protected void ApiAcceptsLogin(UserResponse? user = null, long expiresIn = 3600)
    {
        Api.OnJson(HttpMethod.Post, "/api/auth/login", HttpStatusCode.OK, new
        {
            tokenType = "Bearer",
            accessToken = FakeAccessToken,
            expiresIn,
            refreshToken = "fake-refresh-token"
        });
        Api.OnJson(HttpMethod.Get, "/api/account/me", HttpStatusCode.OK, user ?? SampleUser());
    }

    /// <summary>Logs in through the real form so the client holds an auth cookie.</summary>
    protected async Task LogInAsync()
    {
        ApiAcceptsLogin();
        var response = await PostFormAsync("/Account/Login", LoginForm());
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    // ---------- Reading responses ----------

    /// <summary>The page HTML with entities decoded, so "Can't" matches "Can&#x27;t".</summary>
    protected static async Task<string> ReadHtmlAsync(HttpResponseMessage response) =>
        WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

    /// <summary>The opening &lt;form&gt; tag with the given id.</summary>
    protected static string FormTag(string html, string id)
    {
        var match = Regex.Match(html, $"<form[^>]*id=\"{id}\"[^>]*>");
        Assert.True(match.Success, $"No form with id \"{id}\" on the page.");
        return match.Value;
    }

    protected static bool IsHidden(string formTag) => Regex.IsMatch(formTag, @"\shidden(=|\s|>)");

    protected static string? GetSetCookie(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith(name + "=", StringComparison.Ordinal))
            : null;

    /// <summary>
    /// Decrypts the auth cookie with the app's own key so tests can see the
    /// claims and expiry that the controller put in it.
    /// </summary>
    protected AuthenticationTicket ReadAuthTicket(HttpResponseMessage response)
    {
        var header = GetSetCookie(response, AuthCookieName);
        Assert.NotNull(header);

        var value = Uri.UnescapeDataString(header![(AuthCookieName.Length + 1)..].Split(';')[0]);

        var options = Factory.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        var ticket = options.TicketDataFormat.Unprotect(value);
        Assert.NotNull(ticket);
        return ticket!;
    }

    protected static string? Claim(AuthenticationTicket ticket, string type) =>
        ticket.Principal.FindFirst(type)?.Value;

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryTokenRegex();
}
