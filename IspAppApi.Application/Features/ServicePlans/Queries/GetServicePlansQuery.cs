using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;

namespace IspAppApi.Application.Features.ServicePlans.Queries
{
    public record GetServicePlansQuery() : IRequest<List<ServicePlan>>;

    public class GetServicePlansQueryHandler : IRequestHandler<GetServicePlansQuery, List<ServicePlan>>
    {
        private readonly IServicePlanRepository _servicePlanRepository;

        public GetServicePlansQueryHandler(IServicePlanRepository servicePlanRepository)
        {
            _servicePlanRepository = servicePlanRepository;
        }

        public async Task<List<ServicePlan>> Handle(GetServicePlansQuery request, CancellationToken cancellationToken)
        {
            return await _servicePlanRepository.GetServicePlansAsync();
        }
    }
}
