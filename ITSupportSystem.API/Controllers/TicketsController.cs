using Microsoft.AspNetCore.Mvc;
using ITSupportSystem.Core.Entities;
using ITSupportSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using ITSupportSystem.Core.DTOs.Tickets;
using System.Security.Claims;
using ITSupportSystem.Core.Constants;

namespace ITSupportSystem.API.Controllers;

/// <summary>
/// Ticket endpoints. Every action requires a signed-in user.
/// Regular users work with their own tickets. Staff and Admins can see and edit any ticket.
/// Only Staff and Admins can change a ticket's status, and only Admins can delete tickets.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>
///     <term>GET api/tickets</term>
///     <description>
///     Staff or Admin. Returns every ticket as a list of <see cref="TicketResponse"/>.
///     </description>
///   </item>
///   <item>
///     <term>GET api/tickets/mine</term>
///     <description>
///     Any authenticated user. Returns the caller's own tickets as a list of <see cref="TicketResponse"/>.
///     </description>
///   </item>
///   <item>
///     <term>GET api/tickets/{id}</term>
///     <description>
///     The ticket's owner, Staff or Admin. Returns one ticket as a <see cref="TicketResponse"/>.
///     Returns 403 if the caller does not own the ticket and is not Staff or Admin, or 404 if not found.
///     </description>
///   </item>
///   <item>
///     <term>POST api/tickets</term>
///     <description>
///     Any authenticated user. Creates a ticket from a <see cref="CreateTicketRequest"/>.
///     The caller becomes the owner, and the status starts as <see cref="Status.New"/>.
///     Returns 201 Created (Location points to <c>GetById</c>) or 400 if validation fails.
///     </description>
///   </item>
///   <item>
///     <term>PUT api/tickets/{id}</term>
///     <description>
///     The ticket's owner, Staff or Admin. Updates the title, description and priority from an
///     <see cref="UpdateTicketRequest"/>. Does not change status.
///     Returns 204 No Content, 400 if validation fails, 403, or 404.
///     </description>
///   </item>
///   <item>
///     <term>PUT api/tickets/{id}/status</term>
///     <description>
///     Staff or Admin. Changes the ticket's status from an <see cref="UpdateTicketStatusRequest"/>.
///     Returns 204 No Content, 400 if validation fails, or 404.
///     </description>
///   </item>
///   <item>
///     <term>DELETE api/tickets/{id}</term>
///     <description>
///     Admin only. Deletes a ticket. Its comments and logs are deleted with it (cascade delete).
///     Returns 204 No Content or 404.
///     </description>
///   </item>
/// </list>
/// </remarks>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TicketsController(ITicketRepository repository) : ControllerBase
{
    private readonly ITicketRepository _repo = repository;

    [HttpGet]
    [Authorize(Policy = Policies.RequireStaffOrAdmin)]
    public async Task<IActionResult> GetAll()
    {
        var tickets = await _repo.ReadAllAsync();
        
        var response = new List<TicketResponse>();
        foreach (var ticket in tickets)
        {
            response.Add(TicketResponse.FromTicket(ticket));
        }
        
        return Ok(response);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var tickets = await _repo.ReadByUserAsync(userId);

        var response = new List<TicketResponse>();
        foreach (var ticket in tickets)
        {
            response.Add(TicketResponse.FromTicket(ticket));
        }
        
        return Ok(response);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ticket = await _repo.ReadAsync(id);
        if (ticket is null) return NotFound();

        if (!CanAccess(ticket)) return Forbid();

        return Ok(TicketResponse.FromTicket(ticket));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTicketRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var ticket = new Ticket
        {
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority!.Value,
            Status = Status.New,
            UserId = userId
        };

        var created = await _repo.CreateAsync(ticket);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.TicketId },
            TicketResponse.FromTicket(created)
        );
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateTicketRequest request)
    {
        var ticket = await _repo.ReadAsync(id);
        if (ticket is null) return NotFound();

        if (!CanAccess(ticket)) return Forbid();
        
        ticket.Title = request.Title;
        ticket.Description = request.Description;
        ticket.Priority = request.Priority!.Value;

        await _repo.UpdateAsync(ticket);
        return NoContent();
    }

    [HttpPut("{id}/status")]
    [Authorize(Policy = Policies.RequireStaffOrAdmin)]
    public async Task<IActionResult> UpdateStatus(int id, UpdateTicketStatusRequest request)
    {
        var ticket = await _repo.ReadAsync(id);
        if (ticket is null) return NotFound();

        ticket.Status = request.Status!.Value;

        await _repo.UpdateAsync(ticket);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> Delete(int id)
    {
        var ticket = await _repo.ReadAsync(id);
        if (ticket is null) return NotFound();

        await _repo.DeleteAsync(ticket);
        return NoContent();
    }

    // ----- HELPERS -----
    private bool CanAccess(Ticket ticket)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isStaff = User.IsInRole(Roles.Staff) || User.IsInRole(Roles.Admin);
        return ticket.UserId == userId || isStaff;
    }
} // End class