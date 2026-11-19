using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.ServicePlans.Commands
{
    public record UpdateServicePlanCommand(ServicePlan ServicePlan) : IRequest<bool>;

    public class UpdateServicePlanCommandHandler : IRequestHandler<UpdateServicePlanCommand, bool>
    {
        private readonly IServicePlanRepository _servicePlanRepository;

        public UpdateServicePlanCommandHandler(IServicePlanRepository servicePlanRepository)
        {
            _servicePlanRepository = servicePlanRepository;
        }

        public async Task<bool> Handle(UpdateServicePlanCommand request, CancellationToken cancellationToken)
        {
            await _servicePlanRepository.UpdateServicePlanAsync(request.ServicePlan);
            return true;
        }
    }
}
