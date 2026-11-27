using System.Net;
using ITSupportSystem.Web.Tests.Infrastructure;

namespace ITSupportSystem.Web.Tests.Controllers;

/// <summary>GET requests: do the pages render, and with the right pieces?</summary>
public class PageTests : WebTestBase
{
    [Theory]
    [InlineData("/")]
    [InlineData("/Home/Privacy")]
    [InlineData("/Account/Login")]
    [InlineData("/Ticket")]
    public async Task Page_Renders(string url)
    {
        var response = await Client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task UnknownPage_Returns404()
    {
        var response = await Client.GetAsync("/Nope/Nothing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LoginPage_HasBothForms_WithLoginShowing()
    {
        var html = await ReadHtmlAsync(await Client.GetAsync("/Account/Login"));

        Assert.False(IsHidden(FormTag(html, "login-form")));
        Assert.True(IsHidden(FormTag(html, "register-form")));
    }

    [Fact]
    public async Task LoginPage_FormsPostToTheRightActions()
    {
        var html = await ReadHtmlAsync(await Client.GetAsync("/Account/Login"));

        Assert.Contains("action=\"/Account/Login\"", FormTag(html, "login-form"));
        Assert.Contains("action=\"/Account/Register\"", FormTag(html, "register-form"));
    }

    [Fact]
    public async Task LoginPage_LoginTabIsActive()
    {
        var html = await ReadHtmlAsync(await Client.GetAsync("/Account/Login"));

        Assert.Contains("class=\"tab active\" data-target=\"login-form\"", html);
        Assert.DoesNotContain("class=\"tab active\" data-target=\"register-form\"", html);
    }

    [Fact]
    public async Task LoginPage_HasEveryInput()
    {
        var html = await ReadHtmlAsync(await Client.GetAsync("/Account/Login"));

        // Names must match the [Bind(Prefix = ...)] on the POST actions
        Assert.Contains("name=\"Login.Email\"", html);
        Assert.Contains("name=\"Login.Password\"", html);
        Assert.Contains("name=\"Login.RememberMe\"", html);
        Assert.Contains("name=\"Register.FirstName\"", html);
        Assert.Contains("name=\"Register.LastName\"", html);
        Assert.Contains("name=\"Register.Email\"", html);
        Assert.Contains("name=\"Register.Password\"", html);
    }

    [Fact]
    public async Task LoginPage_PasswordInputsAreMasked()
    {
        var html = await ReadHtmlAsync(await Client.GetAsync("/Account/Login"));

        Assert.Matches("<input[^>]*type=\"password\"[^>]*name=\"Login.Password\"", html);
        Assert.Matches("<input[^>]*type=\"password\"[^>]*name=\"Register.Password\"", html);
    }

    [Fact]
    public async Task LoginPage_HasAntiforgeryToken()
    {
        var token = await GetAntiforgeryTokenAsync();

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task LoginPage_UsesAuthLayoutAndScript()
    {
        var html = await ReadHtmlAsync(await Client.GetAsync("/Account/Login"));

        Assert.Contains("<title>Login - ITSupportSystem.Web</title>", html);
        Assert.Contains("/css/auth.css", html);
        Assert.Contains("/js/auth.js", html);
    }

    [Theory]
    [InlineData("/css/theme.css")]
    [InlineData("/css/auth.css")]
    [InlineData("/js/auth.js")]
    public async Task StaticFile_IsServed(string url)
    {
        var response = await Client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
