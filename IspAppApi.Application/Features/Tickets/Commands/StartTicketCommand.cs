using IspAppApi.Application.Common;
using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using TaskStatus = IspAppApi.Domain.Entities.TaskStatus;

namespace IspAppApi.Application.Features.Tickets.Commands
{
    public record StartTicketCommand(int Id) : IRequest<Result>;

    public class StartTicketCommandHandler : IRequestHandler<StartTicketCommand, Result>
    {
        private readonly ITicketRepository _ticketRepository;

        public StartTicketCommandHandler(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public async Task<Result> Handle(StartTicketCommand request, CancellationToken cancellationToken)
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

            if (ticket.Status == TaskStatus.Closed)
            {
                return Result.Fail("Cannot start a closed ticket. Please reopen it instead.");
            }

            ticket.Status = TaskStatus.InProgress;
            await _ticketRepository.UpdateTicketAsync(ticket);
            return Result.Ok();
        }
    }
}
