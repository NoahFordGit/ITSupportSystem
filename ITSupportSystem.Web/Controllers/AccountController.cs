using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using ITSupportSystem.Core.DTOs.Users;
using ITSupportSystem.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace ITSupportSystem.Web.Controllers;

public class AccountController(IHttpClientFactory httpClientFactory) : Controller
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public IActionResult Login()
    {
        return View(new AuthPageViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([Bind(Prefix = "Login")] LoginViewModel model)
    {
        var page = new AuthPageViewModel { Login = model};

        if (!ModelState.IsValid)
        {
            return View(page);
        }

        var client = _httpClientFactory.CreateClient("Api");

        // get token from API
        AccessTokenResponse? token;
        try
        {
            var loginResponse = await client.PostAsJsonAsync("api/auth/login",
                new LoginRequest{ Email = model.Email, Password = model.Password});

            if (!loginResponse.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(page);
            }

            token = await loginResponse.Content.ReadFromJsonAsync<AccessTokenResponse>();
            if (token is null)
            {
                ModelState.AddModelError(string.Empty, "Login failed. Please try again.");
                return View(page);
            }
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Can't reach the server. Please try again later.");
            return View(page);
        }

        // use token to locate user
        var meRequest = new HttpRequestMessage(HttpMethod.Get, "api/account/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var meResponse = await client.SendAsync(meRequest);
        if (!meResponse.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, "Login failed. Please try again.");
            return View(page);
        }

        var user = await meResponse.Content.ReadFromJsonAsync<UserResponse>();
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Login failed. Please try again.");
            return View(page);
        }

        // build users identity
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("access_token", token.AccessToken)
        };

        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // write cookie
        // 4. Write the cookie
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn)
            }
        );

        return RedirectToAction("Index", "Ticket");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register([Bind(Prefix = "Register")] RegisterViewModel model)
    {
        var page = new AuthPageViewModel { Register = model };
        
        if (!ModelState.IsValid)
        {
            return ShowRegisterTab(page);
        }

        var request = new RegisterUserRequest
        {
            Email = model.Email,
            UserName = model.Email,
            Password = model.Password,
            FirstName = model.FirstName,
            LastName = model.LastName,
        };

        HttpResponseMessage response;
        try
        {
            var client = _httpClientFactory.CreateClient("Api");
            response = await client.PostAsJsonAsync("api/account/register", request);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Can't reach server. Please try again later.");
            return ShowRegisterTab(page);
        }

        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Account Created. You can log in now.";
            return RedirectToAction(nameof(Login));
        }

        await AddApiErrorsAsync(response);
        return ShowRegisterTab(page);
    }


    // ----- Helpers -----
    private ViewResult ShowRegisterTab(AuthPageViewModel page)
    {
        ViewData["ActiveTab"] = "register";
        return View("Login", page);
    }

    private async Task AddApiErrorsAsync(HttpResponseMessage response)
    {
        try
        {
            var errors = await response.Content.ReadFromJsonAsync<List<IdentityError>>();
            foreach (var error in errors ?? [])
            {
                var message = error.Code == "DuplicateUserName"
                    ? "An account with this email already exists"
                    : error.Description;

                ModelState.AddModelError(string.Empty, message);
            }
        }
        catch (JsonException)
        {
            ModelState.AddModelError(string.Empty, "Registration Failed. Please check your details");
        }
    }
}