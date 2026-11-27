using System.Net;
using System.Net.Http.Json;
using ITSupportSystem.API.Data;
using ITSupportSystem.API.Tests.Infrastructure;
using ITSupportSystem.Core.Constants;
using ITSupportSystem.Core.DTOs.Tickets;
using ITSupportSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ITSupportSystem.API.Tests.Controllers;

public class TicketsControllerTests : ApiTestBase
{
    private static CreateTicketRequest NewCreateTicketRequest(string title = "VPN won't connect") => new()
    {
        Title = title,
        Description = "Error 809 since this morning",
        Priority = Priority.High
    };

    private static UpdateTicketRequest NewUpdateTicketRequest(string title = "Updated title") => new()
    {
        Title = title,
        Description = "Updated description",
        Priority = Priority.Low
    };

    /// <summary>Creates a ticket as the given client and returns the response body.</summary>
    private static async Task<TicketResponse> CreateTicketAsync(HttpClient client, string title = "VPN won't connect")
    {
        var response = await client.PostAsJsonAsync("/api/tickets", NewCreateTicketRequest(title));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.NotNull(body);
        return body!;
    }

    /// <summary>Reads a ticket as the seeded admin, who can see every ticket.</summary>
    private async Task<TicketResponse> GetTicketAsAdminAsync(int id)
    {
        using var admin = await LoginAsAdminAsync();
        var ticket = await admin.GetFromJsonAsync<TicketResponse>($"/api/tickets/{id}");
        Assert.NotNull(ticket);
        return ticket!;
    }

    // ---------- Authorization matrix ----------
    // Who gets in where, without caring about the response body.

    public static TheoryData<string, string> StaffOrAdminEndpoints => new()
    {
        { "GET", "/api/tickets" },
        { "PUT", "/api/tickets/1/status" },
    };

    public static TheoryData<string, string> AdminOnlyEndpoints => new()
    {
        { "DELETE", "/api/tickets/1" },
    };

    public static TheoryData<string, string> AnyUserEndpoints => new()
    {
        { "GET", "/api/tickets/mine" },
        { "GET", "/api/tickets/1" },
        { "POST", "/api/tickets" },
        { "PUT", "/api/tickets/1" },
    };

    private static HttpRequestMessage BuildRequest(string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST") request.Content = JsonContent.Create(NewCreateTicketRequest());
        if (method is "PUT" && url.EndsWith("/status")) request.Content = JsonContent.Create(new UpdateTicketStatusRequest { Status = Status.Open });
        else if (method is "PUT") request.Content = JsonContent.Create(NewUpdateTicketRequest());
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

    // ---------- POST /api/tickets ----------

    [Fact]
    public async Task Create_AsUser_Returns201WithLocationAndBody()
    {
        var (client, user) = await CreateAndLoginAsync("owner", Roles.User);

        var response = await client.PostAsJsonAsync("/api/tickets", NewCreateTicketRequest("Printer jam"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.NotNull(body);
        Assert.True(body!.TicketId > 0);
        Assert.Equal("Printer jam", body.Title);
        Assert.Equal("Error 809 since this morning", body.Description);
        Assert.Equal(Priority.High, body.Priority);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal($"/api/tickets/{body.TicketId}", response.Headers.Location!.AbsolutePath, ignoreCase: true);

        // Following the Location header returns the same ticket
        var followed = await client.GetFromJsonAsync<TicketResponse>(response.Headers.Location);
        Assert.Equal(body.TicketId, followed!.TicketId);
    }

    [Fact]
    public async Task Create_SetsServerOwnedFields()
    {
        var (client, user) = await CreateAndLoginAsync("owner", Roles.User);
        var before = DateTime.UtcNow.AddMinutes(-1);

        var ticket = await CreateTicketAsync(client);

        Assert.Equal(Status.New, ticket.Status);
        Assert.Equal(user.Id, ticket.UserId);
        Assert.True(ticket.CreatedAt > before);
        Assert.True(ticket.UpdatedAt > before);
    }

    [Fact]
    public async Task Create_IgnoresClientSuppliedStatusAndUserId()
    {
        var (client, user) = await CreateAndLoginAsync("owner", Roles.User);

        // Extra fields that aren't on CreateTicketRequest must not reach the entity (over-posting).
        var response = await client.PostAsJsonAsync("/api/tickets", new
        {
            title = "Sneaky",
            description = "Trying to set server-owned fields",
            priority = "Low",
            status = "Resolved",
            userId = "someone-else"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(Status.New, body!.Status);
        Assert.Equal(user.Id, body.UserId);
    }

    [Fact]
    public async Task Create_SerializesEnumsAsNames()
    {
        var (client, _) = await CreateAndLoginAsync("owner", Roles.User);

        var response = await client.PostAsJsonAsync("/api/tickets", NewCreateTicketRequest());
        var json = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"priority\":\"High\"", json);
        Assert.Contains("\"status\":\"New\"", json);
    }

    public static TheoryData<string, object> InvalidCreateBodies => new()
    {
        { "missing priority", new { title = "No priority", description = "x" } },
        { "missing title", new { description = "x", priority = "High" } },
        { "blank title", new { title = "   ", description = "x", priority = "High" } },
        { "title too long", new { title = new string('a', 257), description = "x", priority = "High" } },
        { "description too long", new { title = "Long", description = new string('a', 257), priority = "High" } },
        { "unknown priority name", new { title = "Bad", description = "x", priority = "Urgent" } },
        { "undefined priority number", new { title = "Bad", description = "x", priority = 99 } },
    };

    [Theory]
    [MemberData(nameof(InvalidCreateBodies))]
    public async Task Create_InvalidBody_Returns400(string _, object body)
    {
        var (client, _) = await CreateAndLoginAsync("owner", Roles.User);

        var response = await client.PostAsJsonAsync("/api/tickets", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- GET /api/tickets ----------

    [Fact]
    public async Task GetAll_AsStaff_ReturnsEveryUsersTickets()
    {
        var (alice, _) = await CreateAndLoginAsync("alice", Roles.User);
        var (bob, _) = await CreateAndLoginAsync("bob", Roles.User);
        var a = await CreateTicketAsync(alice, "Alice's ticket");
        var b = await CreateTicketAsync(bob, "Bob's ticket");
        var (staff, _) = await CreateAndLoginAsync("staffer", Roles.Staff);

        var tickets = await staff.GetFromJsonAsync<List<TicketResponse>>("/api/tickets");

        Assert.NotNull(tickets);
        Assert.Equal(2, tickets!.Count);
        Assert.Contains(tickets, t => t.TicketId == a.TicketId);
        Assert.Contains(tickets, t => t.TicketId == b.TicketId);
    }

    [Fact]
    public async Task GetAll_AsAdmin_Returns200()
    {
        using var admin = await LoginAsAdminAsync();

        var response = await admin.GetAsync("/api/tickets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- GET /api/tickets/mine ----------

    [Fact]
    public async Task GetMine_ReturnsOnlyCallersTickets()
    {
        var (alice, aliceUser) = await CreateAndLoginAsync("alice", Roles.User);
        var (bob, _) = await CreateAndLoginAsync("bob", Roles.User);
        await CreateTicketAsync(alice, "Alice 1");
        await CreateTicketAsync(alice, "Alice 2");
        await CreateTicketAsync(bob, "Bob 1");

        var tickets = await alice.GetFromJsonAsync<List<TicketResponse>>("/api/tickets/mine");

        Assert.NotNull(tickets);
        Assert.Equal(2, tickets!.Count);
        Assert.All(tickets, t => Assert.Equal(aliceUser.Id, t.UserId));
    }

    [Fact]
    public async Task GetMine_NoTickets_ReturnsEmptyList()
    {
        var (client, _) = await CreateAndLoginAsync("newuser", Roles.User);

        var tickets = await client.GetFromJsonAsync<List<TicketResponse>>("/api/tickets/mine");

        Assert.NotNull(tickets);
        Assert.Empty(tickets!);
    }

    // ---------- GET /api/tickets/{id} ----------

    [Fact]
    public async Task GetById_AsOwner_Returns200()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var created = await CreateTicketAsync(owner);

        var ticket = await owner.GetFromJsonAsync<TicketResponse>($"/api/tickets/{created.TicketId}");

        Assert.NotNull(ticket);
        Assert.Equal(created.TicketId, ticket!.TicketId);
        Assert.Equal(created.Title, ticket.Title);
    }

    [Fact]
    public async Task GetById_AsOtherUser_Returns403()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var (other, _) = await CreateAndLoginAsync("other", Roles.User);
        var created = await CreateTicketAsync(owner);

        var response = await other.GetAsync($"/api/tickets/{created.TicketId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Staff)]
    [InlineData(Roles.Admin)]
    public async Task GetById_AsStaffOrAdmin_Returns200ForAnyTicket(string role)
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var created = await CreateTicketAsync(owner);
        var (privileged, _) = await CreateAndLoginAsync("privileged", role);

        var response = await privileged.GetAsync($"/api/tickets/{created.TicketId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Missing_Returns404()
    {
        var (client, _) = await CreateAndLoginAsync("owner", Roles.User);

        var response = await client.GetAsync("/api/tickets/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- PUT /api/tickets/{id} ----------

    [Fact]
    public async Task Update_AsOwner_Returns204AndPersistsChanges()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var created = await CreateTicketAsync(owner);

        var response = await owner.PutAsJsonAsync($"/api/tickets/{created.TicketId}", NewUpdateTicketRequest("New title"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updated = await GetTicketAsAdminAsync(created.TicketId);
        Assert.Equal("New title", updated.Title);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal(Priority.Low, updated.Priority);
        Assert.True(updated.UpdatedAt >= created.UpdatedAt);
        Assert.Equal(created.UserId, updated.UserId);
    }

    [Fact]
    public async Task Update_DoesNotResetStatus()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var (staff, _) = await CreateAndLoginAsync("staffer", Roles.Staff);
        var created = await CreateTicketAsync(owner);
        await staff.PutAsJsonAsync($"/api/tickets/{created.TicketId}/status", new UpdateTicketStatusRequest { Status = Status.InProgress });

        await owner.PutAsJsonAsync($"/api/tickets/{created.TicketId}", NewUpdateTicketRequest());

        var updated = await GetTicketAsAdminAsync(created.TicketId);
        Assert.Equal(Status.InProgress, updated.Status);
    }

    [Fact]
    public async Task Update_AsOtherUser_Returns403AndLeavesTicketUnchanged()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var (other, _) = await CreateAndLoginAsync("other", Roles.User);
        var created = await CreateTicketAsync(owner, "Original");

        var response = await other.PutAsJsonAsync($"/api/tickets/{created.TicketId}", NewUpdateTicketRequest("Hijacked"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var unchanged = await GetTicketAsAdminAsync(created.TicketId);
        Assert.Equal("Original", unchanged.Title);
    }

    [Theory]
    [InlineData(Roles.Staff)]
    [InlineData(Roles.Admin)]
    public async Task Update_AsStaffOrAdmin_Returns204ForAnyTicket(string role)
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var created = await CreateTicketAsync(owner);
        var (privileged, _) = await CreateAndLoginAsync("privileged", role);

        var response = await privileged.PutAsJsonAsync($"/api/tickets/{created.TicketId}", NewUpdateTicketRequest("Fixed by staff"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updated = await GetTicketAsAdminAsync(created.TicketId);
        Assert.Equal("Fixed by staff", updated.Title);
    }

    [Fact]
    public async Task Update_Missing_Returns404()
    {
        var (client, _) = await CreateAndLoginAsync("owner", Roles.User);

        var response = await client.PutAsJsonAsync("/api/tickets/9999", NewUpdateTicketRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_MissingPriority_Returns400()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var created = await CreateTicketAsync(owner);

        var response = await owner.PutAsJsonAsync($"/api/tickets/{created.TicketId}", new { title = "No priority", description = "x" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- PUT /api/tickets/{id}/status ----------

    [Theory]
    [InlineData(Roles.Staff)]
    [InlineData(Roles.Admin)]
    public async Task UpdateStatus_AsStaffOrAdmin_Returns204AndPersists(string role)
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var created = await CreateTicketAsync(owner, "Keep this title");
        var (privileged, _) = await CreateAndLoginAsync("privileged", role);

        var response = await privileged.PutAsJsonAsync(
            $"/api/tickets/{created.TicketId}/status",
            new UpdateTicketStatusRequest { Status = Status.Resolved });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updated = await GetTicketAsAdminAsync(created.TicketId);
        Assert.Equal(Status.Resolved, updated.Status);
        Assert.Equal("Keep this title", updated.Title);
    }

    [Fact]
    public async Task UpdateStatus_AsOwner_Returns403()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var created = await CreateTicketAsync(owner);

        var response = await owner.PutAsJsonAsync(
            $"/api/tickets/{created.TicketId}/status",
            new UpdateTicketStatusRequest { Status = Status.Resolved });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var unchanged = await GetTicketAsAdminAsync(created.TicketId);
        Assert.Equal(Status.New, unchanged.Status);
    }

    [Fact]
    public async Task UpdateStatus_MissingStatus_Returns400AndDoesNotResetToNew()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var (staff, _) = await CreateAndLoginAsync("staffer", Roles.Staff);
        var created = await CreateTicketAsync(owner);
        await staff.PutAsJsonAsync($"/api/tickets/{created.TicketId}/status", new UpdateTicketStatusRequest { Status = Status.Resolved });

        var response = await staff.PutAsJsonAsync($"/api/tickets/{created.TicketId}/status", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var unchanged = await GetTicketAsAdminAsync(created.TicketId);
        Assert.Equal(Status.Resolved, unchanged.Status);
    }

    [Fact]
    public async Task UpdateStatus_UnknownStatusName_Returns400()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var (staff, _) = await CreateAndLoginAsync("staffer", Roles.Staff);
        var created = await CreateTicketAsync(owner);

        var response = await staff.PutAsJsonAsync($"/api/tickets/{created.TicketId}/status", new { status = "Finished" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_Missing_Returns404()
    {
        var (staff, _) = await CreateAndLoginAsync("staffer", Roles.Staff);

        var response = await staff.PutAsJsonAsync("/api/tickets/9999/status", new UpdateTicketStatusRequest { Status = Status.Open });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- DELETE /api/tickets/{id} ----------

    [Fact]
    public async Task Delete_AsAdmin_Returns204AndRemovesTicket()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var created = await CreateTicketAsync(owner);
        using var admin = await LoginAsAdminAsync();

        var response = await admin.DeleteAsync($"/api/tickets/{created.TicketId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var after = await admin.GetAsync($"/api/tickets/{created.TicketId}");
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }

    [Fact]
    public async Task Delete_AsOwner_Returns403()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var created = await CreateTicketAsync(owner);

        var response = await owner.DeleteAsync($"/api/tickets/{created.TicketId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Missing_Returns404()
    {
        using var admin = await LoginAsAdminAsync();

        var response = await admin.DeleteAsync("/api/tickets/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_CascadesToCommentsAndLogs()
    {
        var (owner, ownerUser) = await CreateAndLoginAsync("owner", Roles.User);
        var created = await CreateTicketAsync(owner);

        // There are no comment/log endpoints yet, so insert them straight into the database.
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Comments.Add(new Comment { TicketId = created.TicketId, UserId = ownerUser.Id, Content = "A comment" });
            db.Logs.Add(new Log { TicketId = created.TicketId, UserId = ownerUser.Id, LogAction = "Created" });
            await db.SaveChangesAsync();
        }

        using var admin = await LoginAsAdminAsync();
        var response = await admin.DeleteAsync($"/api/tickets/{created.TicketId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var check = Factory.Services.CreateScope();
        var checkDb = check.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await checkDb.Comments.AnyAsync(c => c.TicketId == created.TicketId));
        Assert.False(await checkDb.Logs.AnyAsync(l => l.TicketId == created.TicketId));
    }
}
