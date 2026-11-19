using IspAppApi.Application.Interfaces;
using MediatR;

namespace IspAppApi.Application.Features.Bills.Commands
{
    public record DeleteBillCommand(int CustomerId, int BillId) : IRequest<bool>;

    public class DeleteBillCommandHandler : IRequestHandler<DeleteBillCommand, bool>
    {
        private readonly IBillRepository _billRepository;

        public DeleteBillCommandHandler(IBillRepository billRepository)
        {
            _billRepository = billRepository;
        }

        public async Task<bool> Handle(DeleteBillCommand request, CancellationToken cancellationToken)
        {
            var bill = await _billRepository.GetBillAsync(request.CustomerId, request.BillId);
            if (bill == null)
            {
                return false;
            }

            await _billRepository.DeleteBillAsync(request.CustomerId, request.BillId);
            return true;
        }
    }
}
