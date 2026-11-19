using IspAppApi.Application.Common;
using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using TaskStatus = IspAppApi.Domain.Entities.TaskStatus;

namespace IspAppApi.Application.Features.Tickets.Commands
{
    public record DeleteTicketCommand(int Id) : IRequest<Result>;

    public class DeleteTicketCommandHandler : IRequestHandler<DeleteTicketCommand, Result>
    {
        private readonly ITicketRepository _ticketRepository;

        public DeleteTicketCommandHandler(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public async Task<Result> Handle(DeleteTicketCommand request, CancellationToken cancellationToken)
        {
            var ticket = await _ticketRepository.GetTicketAsync(request.Id);
            if (ticket == null)
            {
                return Result.Fail("Ticket not found.");
            }

            if (ticket.Status != TaskStatus.New)
            {
                return Result.Fail($"Cannot delete ticket with status '{ticket.Status}'. Only tickets in 'New' status can be deleted.");
            }

            await _ticketRepository.DeleteTicketAsync(ticket);
            return Result.Ok();
        }
    }
}
