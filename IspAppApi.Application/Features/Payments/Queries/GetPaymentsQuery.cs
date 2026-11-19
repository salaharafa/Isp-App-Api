using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.Payments.Queries
{
    public record GetPaymentsQuery() : IRequest<List<Payment>>;

    public class GetPaymentsQueryHandler : IRequestHandler<GetPaymentsQuery, List<Payment>>
    {
        private readonly IPaymentRepository _paymentRepository;

        public GetPaymentsQueryHandler(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public async Task<List<Payment>> Handle(GetPaymentsQuery request, CancellationToken cancellationToken)
        {
            return await _paymentRepository.GetPaymentsAsync();
        }
    }
}
