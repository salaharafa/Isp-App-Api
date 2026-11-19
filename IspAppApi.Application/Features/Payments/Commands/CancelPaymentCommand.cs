using IspAppApi.Application.Common;
using IspAppApi.Application.Interfaces;
using MediatR;

namespace IspAppApi.Application.Features.Payments.Commands
{
    public record CancelPaymentCommand(int Id, string Reason) : IRequest<Result>;

    public class CancelPaymentCommandHandler : IRequestHandler<CancelPaymentCommand, Result>
    {
        private readonly IPaymentRepository _paymentRepository;

        public CancelPaymentCommandHandler(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public async Task<Result> Handle(CancelPaymentCommand request, CancellationToken cancellationToken)
        {
            return await _paymentRepository.CancelPaymentAsync(request.Id, request.Reason);
        }
    }
}
