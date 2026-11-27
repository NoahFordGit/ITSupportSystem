using System.Net;
using System.Net.Http.Json;
using ITSupportSystem.API.Tests.Infrastructure;
using ITSupportSystem.Core.Constants;
using ITSupportSystem.Core.DTOs.Comments;
using ITSupportSystem.Core.DTOs.Tickets;
using ITSupportSystem.Core.Entities;

namespace ITSupportSystem.API.Tests.Controllers;

public class CommentsControllerTests : ApiTestBase
{
    /// <summary>Creates a ticket as the given client and returns the response body.</summary>
    private static async Task<TicketResponse> CreateTicketAsync(HttpClient client, string title = "VPN won't connect")
    {
        var response = await client.PostAsJsonAsync("/api/tickets", new CreateTicketRequest
        {
            Title = title,
            Description = "Error 809 since this morning",
            Priority = Priority.High
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.NotNull(body);
        return body!;
    }

    /// <summary>Posts a comment as the given client and returns the response body.</summary>
    private static async Task<CommentResponse> CreateCommentAsync(HttpClient client, int ticketId, string content = "Have you tried restarting?")
    {
        var response = await client.PostAsJsonAsync("/api/comments", new CreateCommentRequest
        {
            TicketId = ticketId,
            Content = content
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CommentResponse>();
        Assert.NotNull(body);
        return body!;
    }

    /// <summary>Reads a comment as the seeded admin, who can see every comment.</summary>
    private async Task<CommentResponse> GetCommentAsAdminAsync(int id)
    {
        using var admin = await LoginAsAdminAsync();
        var comment = await admin.GetFromJsonAsync<CommentResponse>($"/api/comments/{id}");
        Assert.NotNull(comment);
        return comment!;
    }

    // ---------- Authorization matrix ----------
    // Who gets in where, without caring about the response body.

    public static TheoryData<string, string> StaffOrAdminEndpoints => new()
    {
        { "GET", "/api/comments" },
    };

    public static TheoryData<string, string> AdminOnlyEndpoints => new()
    {
        { "DELETE", "/api/comments/1" },
    };

    public static TheoryData<string, string> AnyUserEndpoints => new()
    {
        { "GET", "/api/comments/ticket/1" },
        { "GET", "/api/comments/1" },
        { "POST", "/api/comments" },
        { "PUT", "/api/comments/1" },
    };

    private static HttpRequestMessage BuildRequest(string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST") request.Content = JsonContent.Create(new CreateCommentRequest { TicketId = 1, Content = "x" });
        if (method is "PUT") request.Content = JsonContent.Create(new UpdateCommentRequest { Content = "x" });
        return request;
    }

    [Theory]
    [MemberData(nameof(StaffOrAdminEndpoints))]
    [MemberData(nameof(AdminOnlyEndpoints))]
    [MemberData(nameof(AnyUserEndpoints))]
    public async Task AnyEndpoint_Anonymous_Returns401(string method, string url)
    {
        var response = await Anonymous.SendAsync(BuildRequest(method, url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(StaffOrAdminEndpoints))]
    [MemberData(nameof(AdminOnlyEndpoints))]
    public async Task StaffOrAdminEndpoints_AsUser_Returns403(string method, string url)
    {
        var (client, _) = await CreateAndLoginAsync("plainuser", Roles.User);

        var response = await client.SendAsync(BuildRequest(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AdminOnlyEndpoints))]
    public async Task AdminOnlyEndpoints_AsStaff_Returns403(string method, string url)
    {
        var (client, _) = await CreateAndLoginAsync("staffer", Roles.Staff);

        var response = await client.SendAsync(BuildRequest(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- POST /api/comments ----------

    [Fact]
    public async Task Create_AsTicketOwner_Returns201WithLocationAndBody()
    {
        var (owner, ownerUser) = await CreateAndLoginAsync("owner", Roles.User);
        var ticket = await CreateTicketAsync(owner);
        var before = DateTime.UtcNow.AddMinutes(-1);

        var response = await owner.PostAsJsonAsync("/api/comments", new CreateCommentRequest
        {
            TicketId = ticket.TicketId,
            Content = "Still broken"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CommentResponse>();
        Assert.NotNull(body);
        Assert.True(body!.CommentId > 0);
        Assert.Equal("Still broken", body.Content);
        Assert.Equal(ticket.TicketId, body.TicketId);
        Assert.Equal(ownerUser.Id, body.UserId);
        Assert.True(body.CreatedOn > before);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal($"/api/comments/{body.CommentId}", response.Headers.Location!.AbsolutePath, ignoreCase: true);

        // Following the Location header returns the same comment
        var followed = await owner.GetFromJsonAsync<CommentResponse>(response.Headers.Location);
        Assert.Equal(body.CommentId, followed!.CommentId);
    }

    [Theory]
    [InlineData(Roles.Staff)]
    [InlineData(Roles.Admin)]
    public async Task Create_AsStaffOrAdmin_Returns201OnAnyTicket(string role)
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var ticket = await CreateTicketAsync(owner);
        var (privileged, privilegedUser) = await CreateAndLoginAsync("privileged", role);

        var comment = await CreateCommentAsync(privileged, ticket.TicketId, "Looking into it");

        Assert.Equal(privilegedUser.Id, comment.UserId);
    }

    [Fact]
    public async Task Create_OnOtherUsersTicket_Returns403()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var (other, _) = await CreateAndLoginAsync("other", Roles.User);
        var ticket = await CreateTicketAsync(owner);

        var response = await other.PostAsJsonAsync("/api/comments", new CreateCommentRequest
        {
            TicketId = ticket.TicketId,
            Content = "Not my ticket"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var comments = await owner.GetFromJsonAsync<List<CommentResponse>>($"/api/comments/ticket/{ticket.TicketId}");
        Assert.Empty(comments!);
    }

    [Fact]
    public async Task Create_MissingTicket_Returns400()
    {
        var (client, _) = await CreateAndLoginAsync("owner", Roles.User);

        var response = await client.PostAsJsonAsync("/api/comments", new CreateCommentRequest
        {
            TicketId = 9999,
            Content = "Where did it go?"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_IgnoresClientSuppliedUserId()
    {
        var (owner, ownerUser) = await CreateAndLoginAsync("owner", Roles.User);
        var ticket = await CreateTicketAsync(owner);

        // userId isn't on CreateCommentRequest, so it must not reach the entity (over-posting).
        var response = await owner.PostAsJsonAsync("/api/comments", new
        {
            ticketId = ticket.TicketId,
            content = "Sneaky",
            userId = "someone-else"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CommentResponse>();
        Assert.Equal(ownerUser.Id, body!.UserId);
    }

    public static TheoryData<string, object> InvalidCreateBodies => new()
    {
        { "missing ticketId", new { content = "No ticket" } },
        { "missing content", new { ticketId = 1 } },
        { "blank content", new { ticketId = 1, content = "   " } },
        { "content too long", new { ticketId = 1, content = new string('a', 257) } },
    };

    [Theory]
    [MemberData(nameof(InvalidCreateBodies))]
    public async Task Create_InvalidBody_Returns400(string _, object body)
    {
        var (client, _) = await CreateAndLoginAsync("owner", Roles.User);
        await CreateTicketAsync(client); // ticket 1 exists, so only validation can fail

        var response = await client.PostAsJsonAsync("/api/comments", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- GET /api/comments ----------

    [Fact]
    public async Task GetAll_AsStaff_ReturnsCommentsOnEveryTicket()
    {
        var (alice, _) = await CreateAndLoginAsync("alice", Roles.User);
        var (bob, _) = await CreateAndLoginAsync("bob", Roles.User);
        var a = await CreateCommentAsync(alice, (await CreateTicketAsync(alice)).TicketId, "Alice's comment");
        var b = await CreateCommentAsync(bob, (await CreateTicketAsync(bob)).TicketId, "Bob's comment");
        var (staff, _) = await CreateAndLoginAsync("staffer", Roles.Staff);

        var comments = await staff.GetFromJsonAsync<List<CommentResponse>>("/api/comments");

        Assert.NotNull(comments);
        Assert.Equal(2, comments!.Count);
        Assert.Contains(comments, c => c.CommentId == a.CommentId);
        Assert.Contains(comments, c => c.CommentId == b.CommentId);
    }

    // ---------- GET /api/comments/ticket/{ticketId} ----------

    [Fact]
    public async Task GetByTicket_AsOwner_ReturnsOnlyThatTicketsCommentsOldestFirst()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var (staff, _) = await CreateAndLoginAsync("staffer", Roles.Staff);
        var ticket = await CreateTicketAsync(owner, "Main ticket");
        var otherTicket = await CreateTicketAsync(owner, "Other ticket");
        var first = await CreateCommentAsync(owner, ticket.TicketId, "First");
        var second = await CreateCommentAsync(staff, ticket.TicketId, "Second");
        await CreateCommentAsync(owner, otherTicket.TicketId, "Elsewhere");

        var comments = await owner.GetFromJsonAsync<List<CommentResponse>>($"/api/comments/ticket/{ticket.TicketId}");

        Assert.NotNull(comments);
        Assert.Equal([first.CommentId, second.CommentId], comments!.Select(c => c.CommentId));
    }

    [Fact]
    public async Task GetByTicket_NoComments_ReturnsEmptyList()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var ticket = await CreateTicketAsync(owner);

        var comments = await owner.GetFromJsonAsync<List<CommentResponse>>($"/api/comments/ticket/{ticket.TicketId}");

        Assert.NotNull(comments);
        Assert.Empty(comments!);
    }

    [Fact]
    public async Task GetByTicket_AsOtherUser_Returns403()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var (other, _) = await CreateAndLoginAsync("other", Roles.User);
        var ticket = await CreateTicketAsync(owner);

        var response = await other.GetAsync($"/api/comments/ticket/{ticket.TicketId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Staff)]
    [InlineData(Roles.Admin)]
    public async Task GetByTicket_AsStaffOrAdmin_Returns200ForAnyTicket(string role)
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var ticket = await CreateTicketAsync(owner);
        await CreateCommentAsync(owner, ticket.TicketId);
        var (privileged, _) = await CreateAndLoginAsync("privileged", role);

        var comments = await privileged.GetFromJsonAsync<List<CommentResponse>>($"/api/comments/ticket/{ticket.TicketId}");

        Assert.Single(comments!);
    }

    [Fact]
    public async Task GetByTicket_MissingTicket_Returns404()
    {
        var (client, _) = await CreateAndLoginAsync("owner", Roles.User);

        var response = await client.GetAsync("/api/comments/ticket/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- GET /api/comments/{id} ----------

    [Fact]
    public async Task GetById_AsAuthor_Returns200()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var ticket = await CreateTicketAsync(owner);
        var created = await CreateCommentAsync(owner, ticket.TicketId, "Mine");

        var comment = await owner.GetFromJsonAsync<CommentResponse>($"/api/comments/{created.CommentId}");

        Assert.Equal(created.CommentId, comment!.CommentId);
        Assert.Equal("Mine", comment.Content);
    }

    [Fact]
    public async Task GetById_AsTicketOwner_CanReadStaffComment()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var (staff, _) = await CreateAndLoginAsync("staffer", Roles.Staff);
        var ticket = await CreateTicketAsync(owner);
        var staffComment = await CreateCommentAsync(staff, ticket.TicketId, "Reply from IT");

        var response = await owner.GetAsync($"/api/comments/{staffComment.CommentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_AsUnrelatedUser_Returns403()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var (other, _) = await CreateAndLoginAsync("other", Roles.User);
        var ticket = await CreateTicketAsync(owner);
        var created = await CreateCommentAsync(owner, ticket.TicketId);

        var response = await other.GetAsync($"/api/comments/{created.CommentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Missing_Returns404()
    {
        var (client, _) = await CreateAndLoginAsync("owner", Roles.User);

        var response = await client.GetAsync("/api/comments/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- PUT /api/comments/{id} ----------

    [Fact]
    public async Task Update_AsAuthor_Returns204AndPersistsOnlyContent()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var ticket = await CreateTicketAsync(owner);
        var created = await CreateCommentAsync(owner, ticket.TicketId, "Original");

        var response = await owner.PutAsJsonAsync($"/api/comments/{created.CommentId}", new UpdateCommentRequest { Content = "Edited" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updated = await GetCommentAsAdminAsync(created.CommentId);
        Assert.Equal("Edited", updated.Content);
        Assert.Equal(created.TicketId, updated.TicketId);
        Assert.Equal(created.UserId, updated.UserId);
        Assert.Equal(created.CreatedOn, updated.CreatedOn);
    }

    [Fact]
    public async Task Update_AsTicketOwnerOnStaffComment_Returns403AndLeavesCommentUnchanged()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var (staff, _) = await CreateAndLoginAsync("staffer", Roles.Staff);
        var ticket = await CreateTicketAsync(owner);
        var staffComment = await CreateCommentAsync(staff, ticket.TicketId, "Reply from IT");

        // Owning the ticket lets you read a comment, but not edit someone else's.
        var response = await owner.PutAsJsonAsync($"/api/comments/{staffComment.CommentId}", new UpdateCommentRequest { Content = "Rewritten" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var unchanged = await GetCommentAsAdminAsync(staffComment.CommentId);
        Assert.Equal("Reply from IT", unchanged.Content);
    }

    [Theory]
    [InlineData(Roles.Staff)]
    [InlineData(Roles.Admin)]
    public async Task Update_AsStaffOrAdmin_Returns204ForAnyComment(string role)
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var ticket = await CreateTicketAsync(owner);
        var created = await CreateCommentAsync(owner, ticket.TicketId, "Original");
        var (privileged, _) = await CreateAndLoginAsync("privileged", role);

        var response = await privileged.PutAsJsonAsync($"/api/comments/{created.CommentId}", new UpdateCommentRequest { Content = "Moderated" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updated = await GetCommentAsAdminAsync(created.CommentId);
        Assert.Equal("Moderated", updated.Content);
    }

    [Fact]
    public async Task Update_Missing_Returns404()
    {
        var (client, _) = await CreateAndLoginAsync("owner", Roles.User);

        var response = await client.PutAsJsonAsync("/api/comments/9999", new UpdateCommentRequest { Content = "x" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Update_BlankContent_Returns400(string content)
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var ticket = await CreateTicketAsync(owner);
        var created = await CreateCommentAsync(owner, ticket.TicketId);

        var response = await owner.PutAsJsonAsync($"/api/comments/{created.CommentId}", new { content });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- DELETE /api/comments/{id} ----------

    [Fact]
    public async Task Delete_AsAdmin_Returns204AndRemovesComment()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var ticket = await CreateTicketAsync(owner);
        var created = await CreateCommentAsync(owner, ticket.TicketId);
        using var admin = await LoginAsAdminAsync();

        var response = await admin.DeleteAsync($"/api/comments/{created.CommentId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var after = await admin.GetAsync($"/api/comments/{created.CommentId}");
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }

    [Fact]
    public async Task Delete_AsAuthor_Returns403()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var ticket = await CreateTicketAsync(owner);
        var created = await CreateCommentAsync(owner, ticket.TicketId);

        var response = await owner.DeleteAsync($"/api/comments/{created.CommentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Missing_Returns404()
    {
        using var admin = await LoginAsAdminAsync();

        var response = await admin.DeleteAsync("/api/comments/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
