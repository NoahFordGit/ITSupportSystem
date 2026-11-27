using System.Net;
using System.Net.Http.Json;
using ITSupportSystem.API.Tests.Infrastructure;
using ITSupportSystem.Core.Constants;
using ITSupportSystem.Core.DTOs.Comments;
using ITSupportSystem.Core.DTOs.Logs;
using ITSupportSystem.Core.DTOs.Tickets;
using ITSupportSystem.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ITSupportSystem.API.Tests.Api;

/// <summary>
/// Tests that look at the API as a whole rather than at one controller:
/// a safety net over every endpoint's authorization, and an end-to-end
/// walk through a ticket's life that crosses every controller.
/// </summary>
public class ApiWideTests : ApiTestBase
{
    // Controller actions that are allowed to run without a signed-in user.
    // Adding an anonymous endpoint should be a deliberate choice, so it has to be listed here.
    private static readonly HashSet<string> AnonymousActions =
    [
        "Account.Register",
    ];

    private List<ControllerActionDescriptor> AllControllerActions()
    {
        var provider = Factory.Services.GetRequiredService<IActionDescriptorCollectionProvider>();
        return provider.ActionDescriptors.Items.OfType<ControllerActionDescriptor>().ToList();
    }

    private static string NameOf(ControllerActionDescriptor action) => $"{action.ControllerName}.{action.ActionName}";

    // ---------- Authorization safety net ----------

    [Fact]
    public void EveryControllerAction_RequiresAuthorization_UnlessAllowlisted()
    {
        // Catches a new action added without an [Authorize] attribute, which would otherwise
        // be public by default (UsersController and AccountController have no class-level [Authorize]).
        var unprotected = AllControllerActions()
            .Where(a => !AnonymousActions.Contains(NameOf(a)))
            .Where(a => !a.EndpointMetadata.OfType<IAuthorizeData>().Any()
                     || a.EndpointMetadata.OfType<IAllowAnonymous>().Any())
            .Select(NameOf)
            .ToList();

        Assert.Empty(unprotected);
    }

    [Fact]
    public void OnlyAllowlistedActions_AllowAnonymous()
    {
        var anonymous = AllControllerActions()
            .Where(a => a.EndpointMetadata.OfType<IAllowAnonymous>().Any())
            .Select(NameOf)
            .ToHashSet();

        Assert.Equal(AnonymousActions, anonymous);
    }

    [Fact]
    public void EveryControllerAction_HasAnHttpMethod()
    {
        // An action without [HttpGet]/[HttpPost]/... matches every verb and breaks OpenAPI generation.
        var missing = AllControllerActions()
            .Where(a => a.ActionConstraints?.OfType<Microsoft.AspNetCore.Mvc.ActionConstraints.HttpMethodActionConstraint>().Any() != true)
            .Select(NameOf)
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public async Task UnknownApiRoute_Returns404()
    {
        using var admin = await LoginAsAdminAsync();

        var response = await admin.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- End-to-end ----------

    [Fact]
    public async Task TicketLifecycle_AcrossTicketsCommentsAndLogs()
    {
        var (customer, customerUser) = await CreateAndLoginAsync("customer", Roles.User);
        var (tech, techUser) = await CreateAndLoginAsync("tech", Roles.Staff);
        using var admin = await LoginAsAdminAsync();

        // 1. The customer opens a ticket.
        var ticketResponse = await customer.PostAsJsonAsync("/api/tickets", new CreateTicketRequest
        {
            Title = "Laptop won't boot",
            Description = "Black screen after the update",
            Priority = Priority.High
        });
        Assert.Equal(HttpStatusCode.Created, ticketResponse.StatusCode);
        var ticket = (await ticketResponse.Content.ReadFromJsonAsync<TicketResponse>())!;
        Assert.Equal(Status.New, ticket.Status);

        // 2. Staff picks it up: logs the action, moves the status, and replies.
        var logResponse = await tech.PostAsJsonAsync("/api/logs", new CreateLogRequest
        {
            TicketId = ticket.TicketId,
            LogAction = "Assigned",
            Description = "Taken by tech"
        });
        Assert.Equal(HttpStatusCode.Created, logResponse.StatusCode);
        var log = (await logResponse.Content.ReadFromJsonAsync<LogResponse>())!;
        Assert.Equal(techUser.Id, log.UserId);

        var statusResponse = await tech.PutAsJsonAsync(
            $"/api/tickets/{ticket.TicketId}/status",
            new UpdateTicketStatusRequest { Status = Status.InProgress });
        Assert.Equal(HttpStatusCode.NoContent, statusResponse.StatusCode);

        var reply = await tech.PostAsJsonAsync("/api/comments", new CreateCommentRequest
        {
            TicketId = ticket.TicketId,
            Content = "Please hold the power button for 30 seconds."
        });
        Assert.Equal(HttpStatusCode.Created, reply.StatusCode);

        // 3. The customer sees the new status and the reply, and answers.
        var seenTicket = await customer.GetFromJsonAsync<TicketResponse>($"/api/tickets/{ticket.TicketId}");
        Assert.Equal(Status.InProgress, seenTicket!.Status);

        var answer = await customer.PostAsJsonAsync("/api/comments", new CreateCommentRequest
        {
            TicketId = ticket.TicketId,
            Content = "That worked, thanks!"
        });
        Assert.Equal(HttpStatusCode.Created, answer.StatusCode);

        var thread = await customer.GetFromJsonAsync<List<CommentResponse>>($"/api/comments/ticket/{ticket.TicketId}");
        Assert.Equal([techUser.Id, customerUser.Id], thread!.Select(c => c.UserId));

        // 4. ...but the customer can't see the staff-only logs.
        var customerLogs = await customer.GetAsync($"/api/logs/ticket/{ticket.TicketId}");
        Assert.Equal(HttpStatusCode.Forbidden, customerLogs.StatusCode);

        // 5. Staff resolves it.
        var resolve = await tech.PutAsJsonAsync(
            $"/api/tickets/{ticket.TicketId}/status",
            new UpdateTicketStatusRequest { Status = Status.Resolved });
        Assert.Equal(HttpStatusCode.NoContent, resolve.StatusCode);

        // 6. An admin deletes the ticket; its comments and logs go with it.
        var delete = await admin.DeleteAsync($"/api/tickets/{ticket.TicketId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/tickets/{ticket.TicketId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/comments/ticket/{ticket.TicketId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/logs/ticket/{ticket.TicketId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/logs/{log.LogId}")).StatusCode);
        Assert.Empty((await admin.GetFromJsonAsync<List<CommentResponse>>("/api/comments"))!);
    }
}
