using Microsoft.AspNetCore.Mvc;
using ITSupportSystem.Core.Interfaces;
using ITSupportSystem.Core.Entities;
using ITSupportSystem.Core.DTOs.Comments;
using System.Security.Claims;
using ITSupportSystem.Core.Constants;
using Microsoft.AspNetCore.Authorization;

namespace ITSupportSystem.API.Controllers;

/// <summary>
/// Comment endpoints. Every action requires a signed-in user.
/// Regular users work with comments on their own tickets. Staff and Admins can see and comment on any ticket.
/// Only a comment's author (or Staff/Admin) can edit it, and only Admins can delete comments.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>
///     <term>GET api/comments</term>
///     <description>
///     Staff or Admin. Returns every comment as a list of <see cref="CommentResponse"/>.
///     </description>
///   </item>
///   <item>
///     <term>GET api/comments/ticket/{ticketId}</term>
///     <description>
///     The ticket's owner, Staff or Admin. Returns all comments on one ticket as a list of
///     <see cref="CommentResponse"/>.
///     Returns 403 if the caller does not own the ticket and is not Staff or Admin, or 404 if the ticket is not found.
///     </description>
///   </item>
///   <item>
///     <term>GET api/comments/{id}</term>
///     <description>
///     The comment's author, the ticket's owner, Staff or Admin. Returns one comment as a
///     <see cref="CommentResponse"/>. Returns 403 or 404 if not found.
///     </description>
///   </item>
///   <item>
///     <term>POST api/comments</term>
///     <description>
///     The ticket's owner, Staff or Admin. Creates a comment from a <see cref="CreateCommentRequest"/>.
///     The caller becomes the author.
///     Returns 201 Created (Location points to <c>GetById</c>), 400 if validation fails or the ticket
///     does not exist, or 403.
///     </description>
///   </item>
///   <item>
///     <term>PUT api/comments/{id}</term>
///     <description>
///     The comment's author, Staff or Admin. Updates the content from an <see cref="UpdateCommentRequest"/>.
///     Returns 204 No Content, 400 if validation fails, 403, or 404.
///     </description>
///   </item>
///   <item>
///     <term>DELETE api/comments/{id}</term>
///     <description>
///     Admin only. Deletes a comment.
///     Returns 204 No Content or 404.
///     </description>
///   </item>
/// </list>
/// </remarks>
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

        var ticket = await _ticketRepo.ReadAsync(request.TicketId!.Value);
        if (ticket is null) return BadRequest("Ticket does not exist");

        if (!CanAccess(ticket.UserId)) return Forbid();

        var comment = new Comment
        {
            Content = request.Content,
        };

        var created = await _commentRepo.CreateAsync(request.TicketId.Value, userId, comment);
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