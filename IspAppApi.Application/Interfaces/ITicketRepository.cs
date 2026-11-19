using IspAppApi.Domain.Entities;

namespace IspAppApi.Application.Interfaces
{
    public interface ITicketRepository
    {
        Task<List<Ticket>> GetTicketsAsync();
        Task<Ticket?> GetTicketAsync(int id);
        Task AddTicketAsync(Ticket ticket);
        Task UpdateTicketAsync(Ticket ticket);
        Task DeleteTicketAsync(Ticket ticket);
    }
}
