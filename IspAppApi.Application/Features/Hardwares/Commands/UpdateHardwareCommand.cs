using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.Hardwares.Commands
{
    public record UpdateHardwareCommand(Hardware Hardware) : IRequest<bool>;

    public class UpdateHardwareCommandHandler : IRequestHandler<UpdateHardwareCommand, bool>
    {
        private readonly IHardwareRepository _hardwareRepository;

        public UpdateHardwareCommandHandler(IHardwareRepository hardwareRepository)
        {
            _hardwareRepository = hardwareRepository;
        }

        public async Task<bool> Handle(UpdateHardwareCommand request, CancellationToken cancellationToken)
        {
            await _hardwareRepository.UpdateHardwareAsync(request.Hardware);
            return true;
        }
    }
}
