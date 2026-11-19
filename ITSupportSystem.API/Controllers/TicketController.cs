using Microsoft.AspNetCore.Mvc;
using ITSupportSystem.Core.Entities;
using ITSupportSystem.API.Data;
using ITSupportSystem.API.Repositories;
// using ITSupportSystem.API.Repositories;

namespace ITSupportSystem.API.Controllers;

[ApiController]
[Route("api/tickets")]

public class TicketController : ControllerBase
{

    private readonly ITicketRepository _repository;

    public TicketController(ITicketRepository repository)
    {
       _repository = repository;
    } // End constructor 


} // End class