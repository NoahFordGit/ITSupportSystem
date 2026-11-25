using Microsoft.AspNetCore.Identity;
using ITSupportSystem.Core.Constants;
using ITSupportSystem.Core.Entities;

namespace ITSupportSystem.API.Data;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    public static async Task SeedAdminAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = services.GetRequiredService<IConfiguration>().GetSection("SeedAdmin");

        // UserName and Email come from appsettings.json. Password is a secret, so it
        // comes from user secrets (development) or an environment variable. See README.
        var userName = GetRequiredSetting(config, "UserName");
        var email = GetRequiredSetting(config, "Email");
        var password = GetRequiredSetting(config, "Password");

        // Already seeded on a previous run
        if (await userManager.FindByNameAsync(userName) is not null) return;

        var admin = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            FirstName = "System",
            LastName = "Administrator"
        };

        var createResult = await userManager.CreateAsync(admin, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Failed to seed admin: " +
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }

        var roleResult = await userManager.AddToRoleAsync(admin, Roles.Admin);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(admin);
            throw new InvalidOperationException(
                "Failed to assign Admin role: " +
                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }
    }

    // Fail at startup with a clear message instead of silently running without an admin.
    private static string GetRequiredSetting(IConfigurationSection section, string key)
    {
        var value = section[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Missing configuration value '{section.Path}:{key}'. " +
                $"For local development run: dotnet user-secrets set \"{section.Path}:{key}\" \"<value>\" " +
                "--project ITSupportSystem.API (see README).");
        }
        return value;
    }
}