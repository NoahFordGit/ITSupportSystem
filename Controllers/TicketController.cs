using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ITSupportSystem.Data;
using ITSupportSystem.Models.Entities;

namespace ITSupportSystem.Controllers;

public class TicketController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    private readonly SignInManager<IdentityUser> _signInManager;


    public TicketController(ApplicationDbContext context, 
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
    } // End method



    public async Task<IActionResult> Index()
    {
        return View();
    } // End method


} // End class