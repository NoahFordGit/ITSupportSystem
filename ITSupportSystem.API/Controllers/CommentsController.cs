using Microsoft.AspNetCore.Mvc;
using ITSupportSystem.Core.Interfaces;
using ITSupportSystem.Core.Entities;
using ITSupportSystem.Core.DTOs.Comments;
using System.Security.Claims;
using ITSupportSystem.Core.Constants;
using Microsoft.AspNetCore.Authorization;

namespace ITSupportSystem.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CommentsController(ICommentRepository commentRepository, ITicketRepository ticketRepository) : ControllerBase
{
    private readonly ICommentRepository _commentRepo = commentRepository;
    private readonly ITicketRepository _ticketRepo = ticketRepository;

    [HttpGet]
    [Authorize(Policy = Policies.RequireStaffOrAdmin)]
    public async Task<IActionResult> GetAll()
    {
        var comments = await _commentRepo.ReadAllAsync();
        
        var response = new List<CommentResponse>();
        foreach (var comment in comments)
        {
            response.Add(CommentResponse.FromComment(comment));
        } // End foreach

        return Ok(response);
    } // End method

    [HttpGet("ticket/{ticketId}")]
    public async Task<IActionResult> GetByTicket(int ticketId)
    {
        var ticket = await _ticketRepo.ReadAsync(ticketId);
        if (ticket is null) return NotFound();

        if (!CanAccess(ticket.UserId)) return Forbid();

        var comments = await _commentRepo.ReadByTicketAsync(ticketId);

        var response = new List<CommentResponse>();
        foreach (var comment in comments)
        {
            response.Add(CommentResponse.FromComment(comment));
        }

        return Ok(response);
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var comment = await _commentRepo.ReadAsync(id);
        if (comment is null) return NotFound();

        var isAuthor = CanAccess(comment.UserId);
        var isTicketOwner = comment.Ticket is not null && CanAccess(comment.Ticket.UserId);

        if (!isAuthor && !isTicketOwner) return Forbid();

        return Ok(CommentResponse.FromComment(comment));
    } // End method

    [HttpPost]
    public async Task<IActionResult> Create(CreateCommentRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var ticket = await _ticketRepo.ReadAsync(request.TicketId);
        if (ticket is null) return BadRequest("Ticket does not exist");

        if (!CanAccess(ticket.UserId)) return Forbid();

        var comment = new Comment
        {
            Content = request.Content,
        };

        var created = await _commentRepo.CreateAsync(request.TicketId, userId, comment);
        if (created is null) return BadRequest("Could not create comment");

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.CommentId },
            CommentResponse.FromComment(created)
        );
    } // End method

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateCommentRequest request)
    {
        var comment = await _commentRepo.ReadAsync(id);
        if (comment is null) return NotFound();

        if (!CanAccess(comment.UserId)) return Forbid();
        
        comment.Content = request.Content;

        var updated = await _commentRepo.UpdateAsync(id, comment);
        if (updated is null) return NotFound();

        return NoContent();
    } // End method

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> Delete(int id)
    {
        var comment = await _commentRepo.ReadAsync(id);
        if (comment is null) return NotFound();

        await _commentRepo.DeleteAsync(comment);
        return NoContent();
    } // End method

    // Helper method
    private bool CanAccess(string ownerId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isStaff = User.IsInRole(Roles.Staff) || User.IsInRole(Roles.Admin);
        return ownerId == userId || isStaff;
    } // End method
} // End class