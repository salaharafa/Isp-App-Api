using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.Bills.Queries
{
    public record GetBillByIdQuery(int CustomerId, int BillId) : IRequest<Bill?>;

    public class GetBillByIdQueryHandler : IRequestHandler<GetBillByIdQuery, Bill?>
    {
        private readonly IBillRepository _billRepository;

        public GetBillByIdQueryHandler(IBillRepository billRepository)
        {
            _billRepository = billRepository;
        }

        public async Task<Bill?> Handle(GetBillByIdQuery request, CancellationToken cancellationToken)
        {
            return await _billRepository.GetBillAsync(request.CustomerId, request.BillId);
        }
    }
}
