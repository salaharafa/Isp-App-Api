using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.Tickets.Queries
{
    public record GetTicketByIdQuery(int Id) : IRequest<Ticket?>;

    public class GetTicketByIdQueryHandler : IRequestHandler<GetTicketByIdQuery, Ticket?>
    {
        private readonly ITicketRepository _ticketRepository;

        public GetTicketByIdQueryHandler(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public async Task<Ticket?> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
        {
            return await _ticketRepository.GetTicketAsync(request.Id);

            
        }
    }
}
