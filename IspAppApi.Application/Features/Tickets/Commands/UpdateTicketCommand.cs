using IspAppApi.Application.Common;
using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace IspAppApi.Application.Features.Tickets.Commands
{
    public record UpdateTicketCommand(int Id, string Name) : IRequest<Result>
    {
        public UpdateTicketCommand(Ticket ticket) : this(ticket.Id, ticket.Name) { }
    }

    public class UpdateTicketCommandHandler : IRequestHandler<UpdateTicketCommand, Result>
    {
        private readonly ITicketRepository _ticketRepository;

        public UpdateTicketCommandHandler(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public async Task<Result> Handle(UpdateTicketCommand request, CancellationToken cancellationToken)
        {
            var ticket = await _ticketRepository.GetTicketAsync(request.Id);
            if (ticket == null)
            {
                return Result.Fail("Ticket not found.");
            }

            ticket.Name = request.Name;
            await _ticketRepository.UpdateTicketAsync(ticket);
            return Result.Ok();
        }
    }
}
