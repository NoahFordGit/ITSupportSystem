using ITSupportSystem.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITSupportSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await userManager.Users
            .Select(u => new { u.Id, u.Email, u.FirstName, u.LastName })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id}", Name = "GetUserById")]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var roles = userManager.GetRolesAsync(user);

        // !!!!!! TODO: ADD USER RESPONSE DTO HERE!!!!!!
        return Ok();
    }
}