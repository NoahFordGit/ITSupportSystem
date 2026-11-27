using Microsoft.AspNetCore.Mvc;
using ITSupportSystem.Core.Interfaces;
using ITSupportSystem.Core.Entities;
using ITSupportSystem.Core.DTOs.Logs;
using System.Security.Claims;
using ITSupportSystem.Core.Constants;
using Microsoft.AspNetCore.Authorization;

namespace ITSupportSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController(ILogRepository repository) : ControllerBase
{

    private readonly ILogRepository _repository = repository;


    public async Task<IActionResult> GetAll()
    {
        var logs = await _repository.ReadAllAsync();

        var response = new List<LogResponse>();
        foreach (var log in logs)
        {
            response.Add(LogResponse.FromLog(log));
        } // End foreach

        return Ok(response);
    } // End method


    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var log = await _repository.ReadAsync(id);
        if (log is null) return NotFound();

        if (!CanAccess(log)) return Forbid();

        return Ok(LogResponse.FromLog(log));
    } // End method



[HttpPost]
    public async Task<IActionResult> Create(CreateLogRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var ticket = await _repository.ReadAsync(request.TicketId);
        var log = new Log
        {
            LogAction = request.LogAction,
            Description = request.Description,
            TicketId = request.TicketId,
            UserId = userId
        };

        var created = await _repository.CreateAsync(request.TicketId, userId, log);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.LogId },
            LogResponse.FromLog(created)
        );
    } // End method


    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateLogRequest request)
    {
        var log = await _repository.ReadAsync(id);
        if (log is null) return NotFound();

        if (!CanAccess(log)) return Forbid();
        
        log.LogAction = request.LogAction;
        log.Description = request.Description;

        await _repository.UpdateAsync(id, log);
        return NoContent();
    } // End method


    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> Delete(int id)
    {
        var log = await _repository.ReadAsync(id);
        if (log is null) return NotFound();

        await _repository.DeleteAsync(log);
        return NoContent();
    } // End method


    // Helper method
    private bool CanAccess(Log log)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isStaff = User.IsInRole(Roles.Staff) || User.IsInRole(Roles.Admin);
        return log.UserId == userId || isStaff;
    } // End method


} // End class