using Microsoft.AspNetCore.Mvc;
using ITSupportSystem.Core.Entities;
using ITSupportSystem.API.Data;
using ITSupportSystem.API.Repositories;
// using ITSupportSystem.API.Repositories;

namespace ITSupportSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController(ITicketRepository repository) : ControllerBase
{

    private readonly ITicketRepository _repository = repository;
} // End class