using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.Tickets.Commands
{
    public record CreateTicketCommand(Ticket Ticket) : IRequest<Ticket>;

    public class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, Ticket>
    {
        private readonly ITicketRepository _ticketRepository;

        public CreateTicketCommandHandler(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public async Task<Ticket> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
        {
            await _ticketRepository.AddTicketAsync(request.Ticket);
            return request.Ticket;
        }
    }
}
