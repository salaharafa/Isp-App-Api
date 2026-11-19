using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.Bills.Queries
{
    public record GetBillsForCustomerQuery(int CustomerId) : IRequest<List<Bill>>;

    public class GetBillsForCustomerQueryHandler : IRequestHandler<GetBillsForCustomerQuery, List<Bill>>
    {
        private readonly IBillRepository _billRepository;

        public GetBillsForCustomerQueryHandler(IBillRepository billRepository)
        {
            _billRepository = billRepository;
        }

        public async Task<List<Bill>> Handle(GetBillsForCustomerQuery request, CancellationToken cancellationToken)
        {
            return await _billRepository.GetBillsForCustomerAsync(request.CustomerId);
        }
    }
}
