using ITSupportSystem.Core.Entities;
using ITSupportSystem.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ITSupportSystem.Core.Constants;
using ITSupportSystem.API.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Set up authorization policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.RequireAdmin, p => p.RequireRole(Roles.Admin))
    .AddPolicy(Policies.RequireStaffOrAdmin, p => p.RequireRole(Roles.Admin, Roles.Staff));

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Seed data
using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedRolesAsync(scope.ServiceProvider);
    await IdentitySeeder.SeedAdminAsync(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

// prefix all login and auth methods with api/auth as opposed to being top level
app.MapGroup("api/auth")
    .MapIdentityApi<ApplicationUser>()
    .AddEndpointFilter(async (context, next) =>
    {
        // Block the built-in register endpoint. Check the matched route pattern,
        // not the request path, so casing or a trailing slash can't get around it.
        var endpoint = context.HttpContext.GetEndpoint() as RouteEndpoint;
        var pattern = endpoint?.RoutePattern.RawText;

        if (pattern is not null && pattern.EndsWith("/register", StringComparison.OrdinalIgnoreCase))
        {
            return Results.NotFound();
        }

        return await next(context);
    });

app.Run();
