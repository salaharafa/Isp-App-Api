using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.Payments.Queries
{
    public record GetPaymentByIdQuery(int Id) : IRequest<Payment?>;

    public class GetPaymentByIdQueryHandler : IRequestHandler<GetPaymentByIdQuery, Payment?>
    {
        private readonly IPaymentRepository _paymentRepository;

        public GetPaymentByIdQueryHandler(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public async Task<Payment?> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
        {
            return await _paymentRepository.GetPaymentAsync(request.Id);
        }
    }
}
