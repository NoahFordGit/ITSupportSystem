using ITSupportSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ITSupportSystem.Services;

public class ApplicationDbContext(DbContextOptions options) : DbContext(options)
{
    
}