using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.ServicePlans.Queries
{
    public record GetServicePlanByIdQuery(int Id) : IRequest<ServicePlan?>;

    public class GetServicePlanByIdQueryHandler : IRequestHandler<GetServicePlanByIdQuery, ServicePlan?>
    {
        private readonly IServicePlanRepository _servicePlanRepository;

        public GetServicePlanByIdQueryHandler(IServicePlanRepository servicePlanRepository)
        {
            _servicePlanRepository = servicePlanRepository;
        }

        public async Task<ServicePlan?> Handle(GetServicePlanByIdQuery request, CancellationToken cancellationToken)
        {
            return await _servicePlanRepository.GetServicePlanAsync(request.Id);
        }
    }
}
