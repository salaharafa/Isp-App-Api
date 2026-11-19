using IspAppApi.Application.Common;
using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.Payments.Commands
{
    public record CreatePaymentCommand(Payment Payment) : IRequest<Result<Payment>>;

    public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, Result<Payment>>
    {
        private readonly IPaymentRepository _paymentRepository;

        public CreatePaymentCommandHandler(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public async Task<Result<Payment>> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
        {
            return await _paymentRepository.AddPaymentAsync(request.Payment);
        }
    }
}
