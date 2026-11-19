using AutoMapper;
using IspAppApi.Application.Features.Subscriptions.Commands;
using IspAppApi.Application.Features.Subscriptions.Queries;
using IspAppApi.Domain.Entities;
using IspAppApi.DTOs.Subscriptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IspAppApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubscriptionsController : ControllerBase
    {
        private readonly IMediator mediator;
        private readonly ILogger<SubscriptionsController> logger;
        private readonly IMapper mapper;

        public SubscriptionsController(
            IMediator mediator,
            ILogger<SubscriptionsController> logger,
            IMapper mapper)
        {
            this.mediator = mediator;
            this.logger = logger;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<SubscriptionReadDTO>>> GetSubscriptions()
        {
            var subscriptions = await mediator.Send(new GetSubscriptionsQuery());
            var response = mapper.Map<List<SubscriptionReadDTO>>(subscriptions);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SubscriptionReadDTO>> GetSubscription(int id)
        {
            var subscription = await mediator.Send(new GetSubscriptionByIdQuery(id));
            if (subscription == null)
            {
                logger.LogWarning($"Subscription {id} not found for get");
                return NotFound();
            }

            var response = mapper.Map<SubscriptionReadDTO>(subscription);
            return Ok(response);
        }

        [HttpGet("customer/{customerId}")]
        public async Task<ActionResult<List<SubscriptionReadDTO>>> GetSubscriptionsForCustomer(int customerId)
        {
            var subscriptions = await mediator.Send(new GetSubscriptionsForCustomerQuery(customerId));
            var response = mapper.Map<List<SubscriptionReadDTO>>(subscriptions);
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult> AddSubscription(SubscriptionCreateDTO subscriptionCreateDto)
        {
            var subscription = mapper.Map<Subscription>(subscriptionCreateDto);
            var result = await mediator.Send(new CreateSubscriptionCommand(subscription));
            if (!result.Success)
            {
                logger.LogWarning($"Failed to create subscription: {result.ErrorMessage}");
                return NotFound(new { message = result.ErrorMessage });
            }

            return StatusCode(201);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateSubscription(int id, SubscriptionUpdateDTO subscriptionUpdateDto)
        {
            var result = await mediator.Send(new UpdateSubscriptionCommand(id, subscriptionUpdateDto.Name));
            if (!result.Success)
            {
                if (result.ErrorMessage == "Subscription not found.")
                {
                    logger.LogWarning($"Subscription {id} not found for update");
                    return NotFound(new { message = result.ErrorMessage });
                }

                return BadRequest(new { message = result.ErrorMessage });
            }

            return NoContent();
        }

        [HttpPost("{id}/cancel")]
        public async Task<ActionResult> CancelSubscription(int id)
        {
            var result = await mediator.Send(new CancelSubscriptionCommand(id));
            if (!result.Success)
            {
                if (result.ErrorMessage == "Subscription not found.")
                {
                    logger.LogWarning($"Subscription {id} not found for cancellation");
                    return NotFound(new { message = result.ErrorMessage });
                }

                return BadRequest(new { message = result.ErrorMessage });
            }

            return NoContent();
        }

        [HttpPut("{id}/suspend")]
        public async Task<ActionResult> SuspendSubscription(int id)
        {
            var result = await mediator.Send(new SuspendSubscriptionCommand(id));
            if (!result.Success)
            {
                if (result.ErrorMessage == "Subscription not found.")
                {
                    logger.LogWarning($"Subscription {id} not found for suspend");
                    return NotFound(new { message = result.ErrorMessage });
                }

                return BadRequest(new { message = result.ErrorMessage });
            }

            return NoContent();
        }

        [HttpPut("{id}/resume")]
        public async Task<ActionResult> ResumeSubscription(int id)
        {
            var result = await mediator.Send(new ResumeSubscriptionCommand(id));
            if (!result.Success)
            {
                if (result.ErrorMessage == "Subscription not found.")
                {
                    logger.LogWarning($"Subscription {id} not found for resume");
                    return NotFound(new { message = result.ErrorMessage });
                }

                return BadRequest(new { message = result.ErrorMessage });
            }

            return NoContent();
        }

        [HttpPost("simulate-monthly-billing")]
        public async Task<ActionResult> SimulateMonthlyBilling(
            [FromQuery] int year, 
            [FromQuery] int month, 
            [FromQuery] int? customerId = null)
        {
            var result = await mediator.Send(new IspAppApi.Application.Features.Bills.Commands.SimulateMonthlyBillingCommand(year, month, customerId));
            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(result.Data);
        }
    }
}
