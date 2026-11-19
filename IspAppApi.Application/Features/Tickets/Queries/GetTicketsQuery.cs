using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.Tickets.Queries
{
    public record GetTicketsQuery() : IRequest<List<Ticket>>;

    public class GetTicketsQueryHandler : IRequestHandler<GetTicketsQuery, List<Ticket>>
    {
        private readonly ITicketRepository _ticketRepository;

        public GetTicketsQueryHandler(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public async Task<List<Ticket>> Handle(GetTicketsQuery request, CancellationToken cancellationToken)
        {
            return await _ticketRepository.GetTicketsAsync();
        }
    }
}
