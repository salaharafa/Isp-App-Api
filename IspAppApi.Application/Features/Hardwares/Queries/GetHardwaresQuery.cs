using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.Hardwares.Queries
{
    public record GetHardwaresQuery() : IRequest<List<Hardware>>;

    public class GetHardwaresQueryHandler : IRequestHandler<GetHardwaresQuery, List<Hardware>>
    {
        private readonly IHardwareRepository _hardwareRepository;

        public GetHardwaresQueryHandler(IHardwareRepository hardwareRepository)
        {
            _hardwareRepository = hardwareRepository;
        }

        public async Task<List<Hardware>> Handle(GetHardwaresQuery request, CancellationToken cancellationToken)
        {
            return await _hardwareRepository.GetHardwaresAsync();
        }
    }
}
