using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using IspAppApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IspAppApi.Infrastructure.Repositories
{
    public class TicketRepository : ITicketRepository
    {
        private readonly IspAppApiDbContext context;

        public TicketRepository(IspAppApiDbContext context)
        {
            this.context = context;
        }

        public async Task<List<Ticket>> GetTicketsAsync()
        {
            return await context.Tickets
                                .Include(t => t.Customer)
                                .ToListAsync();
        }

        public async Task<Ticket?> GetTicketAsync(int id)
        {
            return await context.Tickets
                                .Include(t => t.Customer)
                                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task AddTicketAsync(Ticket ticket)
        {
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();
        }

        public async Task UpdateTicketAsync(Ticket ticket)
        {
            context.Tickets.Update(ticket);
            await context.SaveChangesAsync();
        }

        public async Task DeleteTicketAsync(Ticket ticket)
        {
            context.Tickets.Remove(ticket);
            await context.SaveChangesAsync();
        }
    }
}
