using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.Hardwares.Queries
{
    public record GetHardwaresForCustomerQuery(int CustomerId) : IRequest<List<Hardware>>;

    public class GetHardwaresForCustomerQueryHandler : IRequestHandler<GetHardwaresForCustomerQuery, List<Hardware>>
    {
        private readonly IHardwareRepository _hardwareRepository;

        public GetHardwaresForCustomerQueryHandler(IHardwareRepository hardwareRepository)
        {
            _hardwareRepository = hardwareRepository;
        }

        public async Task<List<Hardware>> Handle(GetHardwaresForCustomerQuery request, CancellationToken cancellationToken)
        {
            return await _hardwareRepository.GetHardwaresForCustomerAsync(request.CustomerId);
        }
    }
}
