using AutoMapper;
using IspAppApi.Application.Features.Payments.Commands;
using IspAppApi.Application.Features.Payments.Queries;
using IspAppApi.Domain.Entities;
using IspAppApi.DTOs.Payments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IspAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    // [ApiVersion(1.0)]
    public class PaymentsController : ControllerBase
    {
        private readonly IMediator mediator;
        private readonly ILogger<PaymentsController> logger;
        private readonly IMapper mapper;

        public PaymentsController(
            IMediator mediator,
            ILogger<PaymentsController> logger,
            IMapper mapper)
        {
            this.mediator = mediator;
            this.logger = logger;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<Payment>>> GetPayments()
        {
            var payments = await mediator.Send(new GetPaymentsQuery());
            var response = mapper.Map<List<PaymentReadDTO>>(payments);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Payment>> GetPayment(int id)
        {
            var payment = await mediator.Send(new GetPaymentByIdQuery(id));
            if (payment == null)
            {
                logger.LogWarning($"Payment {id} not found for get");
                return NotFound();
            }

            var response = mapper.Map<PaymentReadDTO>(payment);
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult> AddPayment(PaymentCreateDTO paymentCreateDto)
        {
            var payment = mapper.Map<Payment>(paymentCreateDto);
            var result = await mediator.Send(new CreatePaymentCommand(payment));
            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            var response = mapper.Map<PaymentReadDTO>(result.Data);
            return CreatedAtAction(nameof(GetPayment), new { id = response.Id }, response);
        }

        [HttpPut("{id}")]
        public ActionResult UpdatePayment(int id)
        {
            return BadRequest(new { message = "Updating a recorded payment is not permitted. To correct a payment, please cancel it with a valid reason and create a replacement payment." });
        }

        [HttpPost("{id}/cancel")]
        public async Task<ActionResult> CancelPayment(int id, [FromBody] PaymentCancelDTO cancelDto)
        {
            if (cancelDto == null || string.IsNullOrWhiteSpace(cancelDto.Reason))
            {
                return BadRequest(new { message = "Cancellation reason is required." });
            }

            var result = await mediator.Send(new CancelPaymentCommand(id, cancelDto.Reason));
            if (!result.Success)
            {
                if (result.ErrorMessage == "Payment not found.")
                {
                    logger.LogWarning($"Payment {id} not found for cancel");
                    return NotFound(new { message = result.ErrorMessage });
                }

                return BadRequest(new { message = result.ErrorMessage });
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public ActionResult DeletePayment(int id)
        {
            return BadRequest(new { message = "Physical deletion of payments is not permitted. Please use POST api/Payments/{id}/cancel to cancel the payment." });
        }
    }
}
