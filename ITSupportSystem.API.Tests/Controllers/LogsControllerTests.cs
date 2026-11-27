using System.Net;
using System.Net.Http.Json;
using ITSupportSystem.API.Tests.Infrastructure;
using ITSupportSystem.Core.Constants;
using ITSupportSystem.Core.DTOs.Logs;
using ITSupportSystem.Core.DTOs.Tickets;
using ITSupportSystem.Core.Entities;

namespace ITSupportSystem.API.Tests.Controllers;

public class LogsControllerTests : ApiTestBase
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

    private static CreateLogRequest NewCreateLogRequest(int ticketId, string action = "Assigned") => new()
    {
        TicketId = ticketId,
        LogAction = action,
        Description = "Assigned to the network team"
    };

    /// <summary>Creates a log as the given (Staff or Admin) client and returns the response body.</summary>
    private static async Task<LogResponse> CreateLogAsync(HttpClient client, int ticketId, string action = "Assigned")
    {
        var response = await client.PostAsJsonAsync("/api/logs", NewCreateLogRequest(ticketId, action));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LogResponse>();
        Assert.NotNull(body);
        return body!;
    }

    /// <summary>Creates a ticket owned by a regular user and returns a logged-in staff client with it.</summary>
    private async Task<(HttpClient Staff, TicketResponse Ticket)> StaffWithTicketAsync()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var ticket = await CreateTicketAsync(owner);
        var (staff, _) = await CreateAndLoginAsync("staffer", Roles.Staff);
        return (staff, ticket);
    }

    // ---------- Authorization matrix ----------
    // The whole controller is Staff/Admin only; DELETE is Admin only.

    public static TheoryData<string, string> StaffOrAdminEndpoints => new()
    {
        { "GET", "/api/logs" },
        { "GET", "/api/logs/ticket/1" },
        { "GET", "/api/logs/1" },
        { "POST", "/api/logs" },
        { "PUT", "/api/logs/1" },
    };

    public static TheoryData<string, string> AdminOnlyEndpoints => new()
    {
        { "DELETE", "/api/logs/1" },
    };

    private static HttpRequestMessage BuildRequest(string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST") request.Content = JsonContent.Create(NewCreateLogRequest(1));
        if (method is "PUT") request.Content = JsonContent.Create(new UpdateLogRequest { LogAction = "x", Description = "x" });
        return request;
    }

    [Theory]
    [MemberData(nameof(StaffOrAdminEndpoints))]
    [MemberData(nameof(AdminOnlyEndpoints))]
    public async Task AnyEndpoint_Anonymous_Returns401(string method, string url)
    {
        var response = await Anonymous.SendAsync(BuildRequest(method, url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(StaffOrAdminEndpoints))]
    [MemberData(nameof(AdminOnlyEndpoints))]
    public async Task AnyEndpoint_AsUser_Returns403(string method, string url)
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

    [Fact]
    public async Task GetByTicket_AsTicketOwner_Returns403()
    {
        // Owning the ticket is not enough: logs are staff-only.
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var ticket = await CreateTicketAsync(owner);

        var response = await owner.GetAsync($"/api/logs/ticket/{ticket.TicketId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- POST /api/logs ----------

    [Theory]
    [InlineData(Roles.Staff)]
    [InlineData(Roles.Admin)]
    public async Task Create_AsStaffOrAdmin_Returns201WithLocationAndBody(string role)
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var ticket = await CreateTicketAsync(owner);
        var (privileged, privilegedUser) = await CreateAndLoginAsync("privileged", role);
        var before = DateTime.UtcNow.AddMinutes(-1);

        var response = await privileged.PostAsJsonAsync("/api/logs", NewCreateLogRequest(ticket.TicketId, "Escalated"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LogResponse>();
        Assert.NotNull(body);
        Assert.True(body!.LogId > 0);
        Assert.Equal("Escalated", body.LogAction);
        Assert.Equal("Assigned to the network team", body.Description);
        Assert.Equal(ticket.TicketId, body.TicketId);
        Assert.Equal(privilegedUser.Id, body.UserId);
        Assert.True(body.LoggedOn > before);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal($"/api/logs/{body.LogId}", response.Headers.Location!.AbsolutePath, ignoreCase: true);

        // Following the Location header returns the same log
        var followed = await privileged.GetFromJsonAsync<LogResponse>(response.Headers.Location);
        Assert.Equal(body.LogId, followed!.LogId);
    }

    [Fact]
    public async Task Create_MissingTicket_Returns400()
    {
        var (staff, _) = await CreateAndLoginAsync("staffer", Roles.Staff);

        var response = await staff.PostAsJsonAsync("/api/logs", NewCreateLogRequest(9999));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_IgnoresClientSuppliedUserIdAndTime()
    {
        var (staff, ticket) = await StaffWithTicketAsync();
        var staffUser = await staff.GetFromJsonAsync<ITSupportSystem.Core.DTOs.Users.UserResponse>("/api/account/me");

        // userId and loggedOn aren't on CreateLogRequest, so they must not reach the entity (over-posting).
        var response = await staff.PostAsJsonAsync("/api/logs", new
        {
            ticketId = ticket.TicketId,
            logAction = "Sneaky",
            description = "Backdated entry",
            userId = "someone-else",
            loggedOn = "2000-01-01T00:00:00Z"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LogResponse>();
        Assert.Equal(staffUser!.Id, body!.UserId);
        Assert.True(body.LoggedOn > DateTime.UtcNow.AddMinutes(-1));
    }

    public static TheoryData<string, object> InvalidCreateBodies => new()
    {
        { "missing ticketId", new { logAction = "x", description = "x" } },
        { "missing logAction", new { ticketId = 1, description = "x" } },
        { "missing description", new { ticketId = 1, logAction = "x" } },
        { "blank logAction", new { ticketId = 1, logAction = "   ", description = "x" } },
        { "logAction too long", new { ticketId = 1, logAction = new string('a', 257), description = "x" } },
        { "description too long", new { ticketId = 1, logAction = "x", description = new string('a', 257) } },
    };

    [Theory]
    [MemberData(nameof(InvalidCreateBodies))]
    public async Task Create_InvalidBody_Returns400(string _, object body)
    {
        var (staff, _) = await StaffWithTicketAsync(); // ticket 1 exists, so only validation can fail

        var response = await staff.PostAsJsonAsync("/api/logs", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- GET /api/logs ----------

    [Fact]
    public async Task GetAll_AsStaff_ReturnsLogsForEveryTicket()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var t1 = await CreateTicketAsync(owner, "First");
        var t2 = await CreateTicketAsync(owner, "Second");
        var (staff, _) = await CreateAndLoginAsync("staffer", Roles.Staff);
        var a = await CreateLogAsync(staff, t1.TicketId);
        var b = await CreateLogAsync(staff, t2.TicketId);

        var logs = await staff.GetFromJsonAsync<List<LogResponse>>("/api/logs");

        Assert.NotNull(logs);
        Assert.Equal(2, logs!.Count);
        Assert.Contains(logs, l => l.LogId == a.LogId);
        Assert.Contains(logs, l => l.LogId == b.LogId);
    }

    // ---------- GET /api/logs/ticket/{ticketId} ----------

    [Fact]
    public async Task GetByTicket_ReturnsOnlyThatTicketsLogsOldestFirst()
    {
        var (owner, _) = await CreateAndLoginAsync("owner", Roles.User);
        var ticket = await CreateTicketAsync(owner, "Main ticket");
        var otherTicket = await CreateTicketAsync(owner, "Other ticket");
        var (staff, _) = await CreateAndLoginAsync("staffer", Roles.Staff);
        var first = await CreateLogAsync(staff, ticket.TicketId, "Opened");
        var second = await CreateLogAsync(staff, ticket.TicketId, "Assigned");
        await CreateLogAsync(staff, otherTicket.TicketId, "Elsewhere");

        var logs = await staff.GetFromJsonAsync<List<LogResponse>>($"/api/logs/ticket/{ticket.TicketId}");

        Assert.NotNull(logs);
        Assert.Equal([first.LogId, second.LogId], logs!.Select(l => l.LogId));
    }

    [Fact]
    public async Task GetByTicket_NoLogs_ReturnsEmptyList()
    {
        var (staff, ticket) = await StaffWithTicketAsync();

        var logs = await staff.GetFromJsonAsync<List<LogResponse>>($"/api/logs/ticket/{ticket.TicketId}");

        Assert.NotNull(logs);
        Assert.Empty(logs!);
    }

    [Fact]
    public async Task GetByTicket_MissingTicket_Returns404()
    {
        var (staff, _) = await CreateAndLoginAsync("staffer", Roles.Staff);

        var response = await staff.GetAsync("/api/logs/ticket/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- GET /api/logs/{id} ----------

    [Fact]
    public async Task GetById_AsStaff_Returns200()
    {
        var (staff, ticket) = await StaffWithTicketAsync();
        var created = await CreateLogAsync(staff, ticket.TicketId, "Opened");

        var log = await staff.GetFromJsonAsync<LogResponse>($"/api/logs/{created.LogId}");

        Assert.Equal(created.LogId, log!.LogId);
        Assert.Equal("Opened", log.LogAction);
    }

    [Fact]
    public async Task GetById_Missing_Returns404()
    {
        var (staff, _) = await CreateAndLoginAsync("staffer", Roles.Staff);

        var response = await staff.GetAsync("/api/logs/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- PUT /api/logs/{id} ----------

    [Fact]
    public async Task Update_AsStaff_Returns204AndChangesOnlyActionAndDescription()
    {
        var (staff, ticket) = await StaffWithTicketAsync();
        var created = await CreateLogAsync(staff, ticket.TicketId, "Opened");

        var response = await staff.PutAsJsonAsync($"/api/logs/{created.LogId}", new UpdateLogRequest
        {
            LogAction = "Reopened",
            Description = "Customer reported it again"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updated = await staff.GetFromJsonAsync<LogResponse>($"/api/logs/{created.LogId}");
        Assert.Equal("Reopened", updated!.LogAction);
        Assert.Equal("Customer reported it again", updated.Description);
        Assert.Equal(created.TicketId, updated.TicketId);
        Assert.Equal(created.UserId, updated.UserId);
        Assert.Equal(created.LoggedOn, updated.LoggedOn);
    }

    [Fact]
    public async Task Update_AsDifferentStaff_Returns204()
    {
        // Current behavior: any Staff member can edit any log, not just their own.
        var (staff, ticket) = await StaffWithTicketAsync();
        var created = await CreateLogAsync(staff, ticket.TicketId);
        var (otherStaff, _) = await CreateAndLoginAsync("otherstaff", Roles.Staff);

        var response = await otherStaff.PutAsJsonAsync($"/api/logs/{created.LogId}", new UpdateLogRequest
        {
            LogAction = "Edited",
            Description = "By a colleague"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Update_Missing_Returns404()
    {
        var (staff, _) = await CreateAndLoginAsync("staffer", Roles.Staff);

        var response = await staff.PutAsJsonAsync("/api/logs/9999", new UpdateLogRequest { LogAction = "x", Description = "x" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_MissingDescription_Returns400AndLeavesLogUnchanged()
    {
        var (staff, ticket) = await StaffWithTicketAsync();
        var created = await CreateLogAsync(staff, ticket.TicketId, "Opened");

        var response = await staff.PutAsJsonAsync($"/api/logs/{created.LogId}", new { logAction = "Changed" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var unchanged = await staff.GetFromJsonAsync<LogResponse>($"/api/logs/{created.LogId}");
        Assert.Equal("Opened", unchanged!.LogAction);
    }

    // ---------- DELETE /api/logs/{id} ----------

    [Fact]
    public async Task Delete_AsAdmin_Returns204AndRemovesLog()
    {
        var (staff, ticket) = await StaffWithTicketAsync();
        var created = await CreateLogAsync(staff, ticket.TicketId);
        using var admin = await LoginAsAdminAsync();

        var response = await admin.DeleteAsync($"/api/logs/{created.LogId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var after = await admin.GetAsync($"/api/logs/{created.LogId}");
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }

    [Fact]
    public async Task Delete_Missing_Returns404()
    {
        using var admin = await LoginAsAdminAsync();

        var response = await admin.DeleteAsync("/api/logs/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
