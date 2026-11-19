using IspAppApi.Application.Interfaces;
using MediatR;

namespace IspAppApi.Application.Features.Hardwares.Commands
{
    public record DeleteHardwareCommand(int Id) : IRequest<bool>;

    public class DeleteHardwareCommandHandler : IRequestHandler<DeleteHardwareCommand, bool>
    {
        private readonly IHardwareRepository _hardwareRepository;

        public DeleteHardwareCommandHandler(IHardwareRepository hardwareRepository)
        {
            _hardwareRepository = hardwareRepository;
        }

        public async Task<bool> Handle(DeleteHardwareCommand request, CancellationToken cancellationToken)
        {
            var hardware = await _hardwareRepository.GetHardwareAsync(request.Id);
            if (hardware == null)
            {
                return false;
            }

            await _hardwareRepository.DeleteHardwareAsync(hardware);
            return true;
        }
    }
}
