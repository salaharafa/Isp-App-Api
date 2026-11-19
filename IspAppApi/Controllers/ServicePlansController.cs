using AutoMapper;
using IspAppApi.Application.Features.ServicePlans.Commands;
using IspAppApi.Application.Features.ServicePlans.Queries;
using IspAppApi.Domain.Entities;
using IspAppApi.DTOs.ServicePlans;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IspAppApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [ApiVersion(1.0)]
    [Authorize]
    public class ServicePlansController : ControllerBase
    {
        private readonly IMediator mediator;
        private readonly ILogger<ServicePlansController> logger;
        private readonly IMapper mapper;

        public ServicePlansController(
            IMediator mediator,
            ILogger<ServicePlansController> logger,
            IMapper mapper)
        {
            this.mediator = mediator;
            this.logger = logger;
            this.mapper = mapper;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<ServicePlan>>> GetServicePlans()
        {
            var plans = await mediator.Send(new GetServicePlansQuery());
            var response = mapper.Map<List<ServicePlanReadDTO>>(plans);
            return Ok(response);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ServicePlan>> GetServicePlan(int id)
        {
            var plan = await mediator.Send(new GetServicePlanByIdQuery(id));
            if (plan == null)
            {
                logger.LogWarning($"ServicePlan {id} not found for get");
                return NotFound();
            }

            var response = mapper.Map<ServicePlanReadDTO>(plan);
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult> AddServicePlan(ServicePlanCreateDTO servicePlanCreateDto)
        {
            var plan = mapper.Map<ServicePlan>(servicePlanCreateDto);
            await mediator.Send(new CreateServicePlanCommand(plan));
            return StatusCode(201);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateServicePlan(int id, ServicePlanUpdateDTO servicePlanUpdateDto)
        {
            var plan = await mediator.Send(new GetServicePlanByIdQuery(id));
            if (plan == null)
            {
                logger.LogWarning($"ServicePlan {id} not found for update");
                return NotFound();
            }

            mapper.Map(servicePlanUpdateDto, plan);
            await mediator.Send(new UpdateServicePlanCommand(plan));
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteServicePlan(int id)
        {
            var deleted = await mediator.Send(new DeleteServicePlanCommand(id));
            if (!deleted)
            {
                logger.LogWarning($"ServicePlan {id} not found for delete");
                return NotFound();
            }

            return NoContent();
        }
    }
}
