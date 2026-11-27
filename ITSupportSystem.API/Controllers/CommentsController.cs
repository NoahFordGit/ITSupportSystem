using Microsoft.AspNetCore.Mvc;
using ITSupportSystem.Core.Interfaces;
using ITSupportSystem.Core.Entities;
using ITSupportSystem.Core.DTOs.Comments;
using System.Security.Claims;
using ITSupportSystem.Core.Constants;
using Microsoft.AspNetCore.Authorization;


namespace ITSupportSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController(ICommentRepository repository) : ControllerBase
{

    private readonly ICommentRepository _repository = repository;


    public async Task<IActionResult> GetAll()
    {
        var comments = await _repository.ReadAllAsync();

        var response = new List<CommentResponse>();
        foreach (var comment in comments)
        {
            response.Add(CommentResponse.FromComment(comment));
        } // End foreach

        return Ok(response);
    } // End method


    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var comment = await _repository.ReadAsync(id);
        if (comment is null) return NotFound();

        if (!CanAccess(comment)) return Forbid();

        return Ok(CommentResponse.FromComment(comment));
    } // End method



    [HttpPost]
    public async Task<IActionResult> Create(CreateCommentRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var ticket = await _repository.ReadAsync(request.TicketId);
        var comment = new Comment
        {
            Content = request.Content,
            TicketId = request.TicketId,
            UserId = userId
        };

        var created = await _repository.CreateAsync(request.TicketId, userId, comment);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.CommentId },
            CommentResponse.FromComment(created)
        );
    } // End method


    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateCommentRequest request)
    {
        var comment = await _repository.ReadAsync(id);
        if (comment is null) return NotFound();

        if (!CanAccess(comment)) return Forbid();
        
        comment.Content = request.Content;

        await _repository.UpdateAsync(id, comment);
        return NoContent();
    } // End method


    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> Delete(int id)
    {
        var comment = await _repository.ReadAsync(id);
        if (comment is null) return NotFound();

        await _repository.DeleteAsync(comment);
        return NoContent();
    } // End method



    // Helper method
    private bool CanAccess(Comment comment)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isStaff = User.IsInRole(Roles.Staff) || User.IsInRole(Roles.Admin);
        return comment.UserId == userId || isStaff;
    } // End method


} // End class