using ITSupportSystem.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ITSupportSystem.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Ticket> Ticket => Set<Ticket>();
    public DbSet<Comment> Comment => Set<Comment>();
    public DbSet<Log> Log => Set<Log>();
}
