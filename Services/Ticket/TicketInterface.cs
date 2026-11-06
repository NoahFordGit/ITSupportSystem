using Microsoft.EntityFrameworkCore;
using ITSupportSystem.Models.Entities;
using ITSupportSystem.Data;

namespace ITSupportSystem.Services;

public class TicketRepository : ITicketRepository
{
    private readonly ApplicationDbContext _context;


    public TicketRepository(ApplicationDbContext context)
    {
        _context = context;
    } // End method

    public Task<Ticket> CreateAsync(Ticket ticket)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteAsync(Ticket ticket)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Ticket>> ReadAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<Ticket?> ReadAsync(int TicketId)
    {
        throw new NotImplementedException();
    }

    public Task<Ticket?> UpdateAsync(Ticket ticket)
    {
        throw new NotImplementedException();
    }
} // End class