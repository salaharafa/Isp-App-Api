using IspAppApi.Application.Common;
using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using TaskStatus = IspAppApi.Domain.Entities.TaskStatus;

namespace IspAppApi.Application.Features.Tickets.Commands
{
    public record CloseTicketCommand(int Id) : IRequest<Result>;

    public class CloseTicketCommandHandler : IRequestHandler<CloseTicketCommand, Result>
    {
        private readonly ITicketRepository _ticketRepository;

        public CloseTicketCommandHandler(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public async Task<Result> Handle(CloseTicketCommand request, CancellationToken cancellationToken)
        {
            var ticket = await _ticketRepository.GetTicketAsync(request.Id);
            if (ticket == null)
            {
                return Result.Fail("Ticket not found.");
            }

            if (ticket.Status == TaskStatus.Closed)
            {
                return Result.Fail("Ticket is already closed.");
            }

            if (ticket.Status == TaskStatus.New)
            {
                return Result.Fail("Cannot close a new ticket directly. It must be in progress first.");
            }

            ticket.Status = TaskStatus.Closed;
            await _ticketRepository.UpdateTicketAsync(ticket);
            return Result.Ok();
        }
    }
}
