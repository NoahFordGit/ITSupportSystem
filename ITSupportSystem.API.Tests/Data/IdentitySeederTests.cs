using ITSupportSystem.API.Data;
using ITSupportSystem.API.Tests.Infrastructure;
using ITSupportSystem.Core.Constants;
using ITSupportSystem.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ITSupportSystem.API.Tests.Data;

/// <summary>
/// Program.cs runs the seeder once on startup. These tests check what it
/// left behind, then run it again to prove it's safe to re-run.
/// </summary>
public class IdentitySeederTests : ApiTestBase
{
    [Fact]
    public async Task Startup_SeedsAllRoles()
    {
        using var scope = Factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var roles = await roleManager.Roles.Select(r => r.Name).ToListAsync();

        Assert.Equal(Roles.All.OrderBy(r => r), roles.OrderBy(r => r));
    }

    [Fact]
    public async Task Startup_SeedsAdminWithExpectedValues()
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var admin = await userManager.FindByNameAsync(AdminUserName);

        Assert.NotNull(admin);
        Assert.Equal(AdminEmail, admin!.Email);
        Assert.True(admin.EmailConfirmed);
        Assert.Equal("System", admin.FirstName);
        Assert.Equal("Administrator", admin.LastName);
        Assert.True(await userManager.CheckPasswordAsync(admin, AdminPassword));
        Assert.Equal([Roles.Admin], await userManager.GetRolesAsync(admin));
    }

    [Fact]
    public async Task Seeders_RunTwice_DoNotDuplicateAnything()
    {
        using (var scope = Factory.Services.CreateScope())
        {
            await IdentitySeeder.SeedRolesAsync(scope.ServiceProvider);
            await IdentitySeeder.SeedAdminAsync(scope.ServiceProvider);
        }

        using var check = Factory.Services.CreateScope();
        var roleManager = check.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = check.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        Assert.Equal(Roles.All.Length, await roleManager.Roles.CountAsync());
        Assert.Equal(1, await userManager.Users.CountAsync());
        Assert.Single(await userManager.GetUsersInRoleAsync(Roles.Admin));
    }

    [Fact]
    public async Task SeedAdmin_WhenAdminUserNameExists_LeavesExistingUserAlone()
    {
        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = await userManager.FindByNameAsync(AdminUserName);
            admin!.FirstName = "Renamed";
            await userManager.UpdateAsync(admin);

            await IdentitySeeder.SeedAdminAsync(scope.ServiceProvider);
        }

        using var check = Factory.Services.CreateScope();
        var users = check.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var after = await users.FindByNameAsync(AdminUserName);
        Assert.Equal("Renamed", after!.FirstName);
    }
}
