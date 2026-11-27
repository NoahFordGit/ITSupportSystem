using ITSupportSystem.Core.Entities;

namespace ITSupportSystem.Core.DTOs.Logs;

public record LogResponse
{
    
    public int LogId {get; set;}

    public string LogAction {get; set;} = string.Empty;

    public string Description {get; set;} = string.Empty;

    public DateTime LoggedOn {get; set;}

    // Ticket field
    public int TicketId {get; set;}

    // User field
    public string UserId {get; set;} = string.Empty;


    public static LogResponse FromLog(Log log) => new()
    {
        LogId = log.LogId,
        LogAction = log.LogAction,
        Description = log.Description,
        LoggedOn = log.LoggedOn,
        TicketId = log.TicketId,
        UserId = log.UserId
    }; // End static

} // End record