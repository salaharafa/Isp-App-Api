using IspAppApi.Application.Common;
using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.Bills.Commands
{
    public record UpdateBillCommand(Bill Bill) : IRequest<Result>;

    public class UpdateBillCommandHandler : IRequestHandler<UpdateBillCommand, Result>
    {
        private readonly IBillRepository _billRepository;

        public UpdateBillCommandHandler(IBillRepository billRepository)
        {
            _billRepository = billRepository;
        }

        public async Task<Result> Handle(UpdateBillCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await _billRepository.UpdateBillAsync(request.Bill);
                return Result.Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message);
            }
        }
    }
}
