using ITSupportSystem.Core.Entities;
using ITSupportSystem.Core.DTOs.Users;
using ITSupportSystem.Core.Constants;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace ITSupportSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController(
    UserManager<ApplicationUser> userManager)
    : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterUserRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded) return BadRequest(createResult.Errors);

        var roleResult = await userManager.AddToRoleAsync(user, Roles.User);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return BadRequest(roleResult.Errors);
        }

        var response = UserResponse.FromUser(user, Roles.User);
        return CreatedAtAction(nameof(GetMe), response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return NotFound();

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;

        return Ok(UserResponse.FromUser(user, role));
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe(UpdateUserRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return NotFound();

        user.UserName = request.UserName;
        user.Email = request.Email;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded) return BadRequest(updateResult.Errors);

        return NoContent();
    }

    [HttpDelete("me")]
    [Authorize]
    public async Task<IActionResult> DeleteMe(DeleteUserRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return NotFound();

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            return BadRequest("Incorrect password.");
        }

        if (await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            var admins = await userManager.GetUsersInRoleAsync(Roles.Admin);
            if (admins.Count <= 1)
            {
                return BadRequest("Cannot remove the last Admin.");
            }
        }

        var deleteResult = await userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded) return BadRequest(deleteResult.Errors);

        return NoContent();
    }

    // ----- Helpers -----
    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = userManager.GetUserId(User);
        if (userId is null) return null;

        return await userManager.FindByIdAsync(userId);
    }
}