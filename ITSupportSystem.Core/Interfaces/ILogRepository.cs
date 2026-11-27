using ITSupportSystem.Core.Entities;

namespace ITSupportSystem.Core.Interfaces;

public interface ILogRepository
{
    Task<IEnumerable<Log>> ReadAllAsync();
    Task<IEnumerable<Log>> ReadByTicketAsync(int ticketId);
    Task<Log?> ReadAsync(int LogId);
    Task<Log?> CreateAsync(int TicketId, string Id, Log log);
    Task<Log?> UpdateAsync(int oldId, Log log);
    Task<bool> DeleteAsync(Log log);

} // End interface