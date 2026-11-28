using ITSupportSystem.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ITSupportSystem.Web.Controllers;

public class TicketController : Controller
{
    
    private readonly IConfiguration _config;

    public TicketController(IConfiguration config)
    {
        _config = config;
    } // End constructor


    public IActionResult Index()
    {
        ViewData["ApiBaseAddress"] = _config["API:BaseUrl"];
        return View();
    } // End method

} // End method