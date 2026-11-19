using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.Hardwares.Commands
{
    public record CreateHardwareCommand(Hardware Hardware) : IRequest<Hardware>;

    public class CreateHardwareCommandHandler : IRequestHandler<CreateHardwareCommand, Hardware>
    {
        private readonly IHardwareRepository _hardwareRepository;

        public CreateHardwareCommandHandler(IHardwareRepository hardwareRepository)
        {
            _hardwareRepository = hardwareRepository;
        }

        public async Task<Hardware> Handle(CreateHardwareCommand request, CancellationToken cancellationToken)
        {
            await _hardwareRepository.AddHardwareAsync(request.Hardware);
            return request.Hardware;
        }
    }
}
