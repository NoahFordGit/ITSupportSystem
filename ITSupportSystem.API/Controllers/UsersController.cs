using ITSupportSystem.Core.Entities;
using ITSupportSystem.Core.DTOs.Users;
using ITSupportSystem.Core.Constants;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace ITSupportSystem.API.Controllers;

/// <summary>
/// User-management endpoints for Staff and Admins. These actions work on any account by id.
/// For a user managing their own account, see <see cref="AccountController"/>.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>
///     <term>GET api/users</term>
///     <description>
///     Staff or Admin. Returns every user with their role as a list of <see cref="UserResponse"/>.
///     </description>
///   </item>
///   <item>
///     <term>GET api/users/{id}</term>
///     <description>
///     Staff or Admin. Returns one user as a <see cref="UserResponse"/>, or 404 if not found.
///     Named route: <c>GetUserById</c>.
///     </description>
///   </item>
///   <item>
///     <term>POST api/users</term>
///     <description>
///     Admin only. Creates a user from a <see cref="CreateUserRequest"/> with the requested role.
///     If the role assignment fails, the new account is deleted.
///     Returns 201 Created (Location points to <c>GetUserById</c>) or 400 with Identity errors.
///     </description>
///   </item>
///   <item>
///     <term>PUT api/users/{id}</term>
///     <description>
///     Admin only. Updates a user's username, email, first/last name and phone number from an
///     <see cref="UpdateUserRequest"/>. Does not change password or role.
///     Returns 204 No Content, 400 with Identity errors, or 404.
///     </description>
///   </item>
///   <item>
///     <term>DELETE api/users/{id}</term>
///     <description>
///     Admin only. Deletes a user. Refuses to delete the last remaining Admin.
///     Returns 204 No Content, 400 (last Admin or Identity errors), or 404.
///     </description>
///   </item>
///   <item>
///     <term>GET api/users/roles</term>
///     <description>
///     Any authenticated user. Returns the names of all roles in the system.
///     </description>
///   </item>
///   <item>
///     <term>PUT api/users/{id}/role</term>
///     <description>
///     Admin only. Replaces a user's role with the one in a <see cref="ChangeRoleRequest"/>.
///     Does nothing if the user already has only that role. Refuses to demote the last remaining Admin.
///     If adding the new role fails, the previous roles are restored.
///     Returns 204 No Content, 400 (last Admin or Identity errors), or 404.
///     </description>
///   </item>
/// </list>
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class UsersController(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager)
    : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.RequireStaffOrAdmin)]
    public async Task<IActionResult> GetAll()
    {
        var users = await userManager.Users.ToListAsync();
        
        var response = new List<UserResponse>();
        foreach(var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? string.Empty;
            response.Add(UserResponse.FromUser(user, role));
        }

        return Ok(response);
    }

    [HttpGet("{id}", Name = "GetUserById")]
    [Authorize(Policy = Policies.RequireStaffOrAdmin)]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;

        return Ok(UserResponse.FromUser(user, role));
    }

    [HttpPost]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> Create(CreateUserRequest request)
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

        var roleResult = await userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return BadRequest(roleResult.Errors);
        }

        var response = UserResponse.FromUser(user, request.Role);
        return CreatedAtRoute("GetUserById", new { id = user.Id }, response);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> Update(string id, UpdateUserRequest request)
    {
        var user = await userManager.FindByIdAsync(id);
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

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

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

    [HttpGet("roles")]
    [Authorize]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await roleManager.Roles
            .Select(r => r.Name)
            .ToListAsync();

        return Ok(roles);
    }

    [HttpPut("{id}/role")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> UpdateRole(string id, ChangeRoleRequest request)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var currentRoles = await userManager.GetRolesAsync(user);

        if (currentRoles.Count == 1 && currentRoles.Contains(request.Role))
        {
            return NoContent();
        }

        if (currentRoles.Contains(Roles.Admin) && request.Role != Roles.Admin)
        {
            var admins = await userManager.GetUsersInRoleAsync(Roles.Admin);
            if (admins.Count <= 1)
            {
                return BadRequest("Cannot remove the last Admin.");
            }
        }

        var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded) return BadRequest(removeResult.Errors);

        var addResult = await userManager.AddToRoleAsync(user, request.Role);
        if (!addResult.Succeeded)
        {
            await userManager.AddToRolesAsync(user, currentRoles); // rollback roles if change fails
            return BadRequest(addResult.Errors);
        }

        return NoContent();
    }
}