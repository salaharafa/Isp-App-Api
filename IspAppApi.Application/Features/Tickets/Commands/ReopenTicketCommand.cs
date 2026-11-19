using IspAppApi.Application.Common;
using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using TaskStatus = IspAppApi.Domain.Entities.TaskStatus;

namespace IspAppApi.Application.Features.Tickets.Commands
{
    public record ReopenTicketCommand(int Id) : IRequest<Result>;

    public class ReopenTicketCommandHandler : IRequestHandler<ReopenTicketCommand, Result>
    {
        private readonly ITicketRepository _ticketRepository;

        public ReopenTicketCommandHandler(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public async Task<Result> Handle(ReopenTicketCommand request, CancellationToken cancellationToken)
        {
            var ticket = await _ticketRepository.GetTicketAsync(request.Id);
            if (ticket == null)
            {
                return Result.Fail("Ticket not found.");
            }

            if (ticket.Status == TaskStatus.InProgress)
            {
                return Result.Fail("Ticket is already in progress.");
            }

            if (ticket.Status == TaskStatus.New)
            {
                return Result.Fail("Cannot reopen a new ticket. It has not been closed yet.");
            }

            ticket.Status = TaskStatus.InProgress;
            await _ticketRepository.UpdateTicketAsync(ticket);
            return Result.Ok();
        }
    }
}
