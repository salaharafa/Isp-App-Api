using IspAppApi.Application.Interfaces;
using MediatR;

namespace IspAppApi.Application.Features.ServicePlans.Commands
{
    public record DeleteServicePlanCommand(int Id) : IRequest<bool>;

    public class DeleteServicePlanCommandHandler : IRequestHandler<DeleteServicePlanCommand, bool>
    {
        private readonly IServicePlanRepository _servicePlanRepository;

        public DeleteServicePlanCommandHandler(IServicePlanRepository servicePlanRepository)
        {
            _servicePlanRepository = servicePlanRepository;
        }

        public async Task<bool> Handle(DeleteServicePlanCommand request, CancellationToken cancellationToken)
        {
            var plan = await _servicePlanRepository.GetServicePlanAsync(request.Id);
            if (plan == null)
            {
                return false;
            }

            await _servicePlanRepository.DeleteServicePlanAsync(plan);
            return true;
        }
    }
}
