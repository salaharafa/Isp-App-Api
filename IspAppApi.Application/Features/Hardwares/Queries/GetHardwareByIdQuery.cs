using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.Hardwares.Queries
{
    public record GetHardwareByIdQuery(int Id) : IRequest<Hardware?>;

    public class GetHardwareByIdQueryHandler : IRequestHandler<GetHardwareByIdQuery, Hardware?>
    {
        private readonly IHardwareRepository _hardwareRepository;

        public GetHardwareByIdQueryHandler(IHardwareRepository hardwareRepository)
        {
            _hardwareRepository = hardwareRepository;
        }

        public async Task<Hardware?> Handle(GetHardwareByIdQuery request, CancellationToken cancellationToken)
        {
            return await _hardwareRepository.GetHardwareAsync(request.Id);
        }
    }
}
