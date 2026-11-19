using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.ServicePlans.Commands
{
    public record CreateServicePlanCommand(ServicePlan ServicePlan) : IRequest<ServicePlan>;

    public class CreateServicePlanCommandHandler : IRequestHandler<CreateServicePlanCommand, ServicePlan>
    {
        private readonly IServicePlanRepository _servicePlanRepository;

        public CreateServicePlanCommandHandler(IServicePlanRepository servicePlanRepository)
        {
            _servicePlanRepository = servicePlanRepository;
        }

        public async Task<ServicePlan> Handle(CreateServicePlanCommand request, CancellationToken cancellationToken)
        {
            await _servicePlanRepository.AddServicePlanAsync(request.ServicePlan);
            return request.ServicePlan;
        }
    }
}
