using Microsoft.AspNetCore.Mvc;
using ITSupportSystem.Core.Interfaces;
using ITSupportSystem.Core.Entities;
using ITSupportSystem.Core.DTOs.Logs;
using System.Security.Claims;
using ITSupportSystem.Core.Constants;
using Microsoft.AspNetCore.Authorization;

namespace ITSupportSystem.API.Controllers;

/// <summary>
/// Log endpoints. Every action requires a signed-in Staff or Admin user; regular users cannot see logs.
/// Logs record actions taken on a ticket. Only Admins can delete logs.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>
///     <term>GET api/logs</term>
///     <description>
///     Staff or Admin. Returns every log as a list of <see cref="LogResponse"/>.
///     </description>
///   </item>
///   <item>
///     <term>GET api/logs/ticket/{ticketId}</term>
///     <description>
///     Staff or Admin. Returns all logs for one ticket, oldest first, as a list of <see cref="LogResponse"/>.
///     Returns 404 if the ticket is not found.
///     </description>
///   </item>
///   <item>
///     <term>GET api/logs/{id}</term>
///     <description>
///     Staff or Admin. Returns one log as a <see cref="LogResponse"/>, or 404 if not found.
///     </description>
///   </item>
///   <item>
///     <term>POST api/logs</term>
///     <description>
///     Staff or Admin. Creates a log from a <see cref="CreateLogRequest"/>. The caller is recorded as the
///     log's user, and the time is set to now (UTC).
///     Returns 201 Created (Location points to <c>GetById</c>), or 400 if validation fails or the ticket
///     does not exist.
///     </description>
///   </item>
///   <item>
///     <term>PUT api/logs/{id}</term>
///     <description>
///     Staff or Admin. Updates the action and description from an <see cref="UpdateLogRequest"/>.
///     Does not change the ticket, user or time.
///     Returns 204 No Content, 400 if validation fails, or 404.
///     </description>
///   </item>
///   <item>
///     <term>DELETE api/logs/{id}</term>
///     <description>
///     Admin only. Deletes a log.
///     Returns 204 No Content or 404.
///     </description>
///   </item>
/// </list>
/// </remarks>
[ApiController]
[Authorize(Policy = Policies.RequireStaffOrAdmin)]
[Route("api/[controller]")]
public class LogsController(ILogRepository logRepository, ITicketRepository ticketRepository) : ControllerBase
{
    private readonly ILogRepository _logRepo = logRepository;
    private readonly ITicketRepository _ticketRepo = ticketRepository;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var logs = await _logRepo.ReadAllAsync();

        var response = new List<LogResponse>();
        foreach (var log in logs)
        {
            response.Add(LogResponse.FromLog(log));
        } // End foreach

        return Ok(response);
    } // End method

    [HttpGet("ticket/{ticketId}")]
    public async Task<IActionResult> GetByTicket(int ticketId)
    {
        var ticket = await _ticketRepo.ReadAsync(ticketId);
        if (ticket is null) return NotFound();

        var logs = await _logRepo.ReadByTicketAsync(ticketId);

        var response = new List<LogResponse>();
        foreach (var log in logs)
        {
            response.Add(LogResponse.FromLog(log));
        }

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var log = await _logRepo.ReadAsync(id);
        if (log is null) return NotFound();

        return Ok(LogResponse.FromLog(log));
    } // End method 

    [HttpPost]
    public async Task<IActionResult> Create(CreateLogRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return BadRequest("User does not exist.");
        
        var ticket = await _ticketRepo.ReadAsync(request.TicketId!.Value);
        if (ticket is null) return BadRequest("Ticket does not exist.");

        var log = new Log
        {
            LogAction = request.LogAction,
            Description = request.Description
        };

        var created = await _logRepo.CreateAsync(request.TicketId.Value, userId, log);
        if (created is null) return BadRequest("Could not create log.");
        
        return CreatedAtAction(
            nameof(GetById),
            new { id = created.LogId },
            LogResponse.FromLog(created)
        );
    } // End method

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateLogRequest request)
    {
        var log = await _logRepo.ReadAsync(id);
        if (log is null) return NotFound();
        
        log.LogAction = request.LogAction;
        log.Description = request.Description;

        var updated = await _logRepo.UpdateAsync(id, log);
        if (updated is null) return NotFound();
        
        return NoContent();
    } // End method

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> Delete(int id)
    {
        var log = await _logRepo.ReadAsync(id);
        if (log is null) return NotFound();

        await _logRepo.DeleteAsync(log);
        return NoContent();
    } // End method
} // End class