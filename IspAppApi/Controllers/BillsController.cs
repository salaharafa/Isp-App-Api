using AutoMapper;
using IspAppApi.Application.Features.Bills.Commands;
using IspAppApi.Application.Features.Bills.Queries;
using IspAppApi.Domain.Entities;
using IspAppApi.DTOs.Bills;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IspAppApi.Controllers
{
    [Route("api/customers/{customerId}/[controller]")]
    [ApiController]
    // [ApiVersion(1.0)]
    [Authorize]
    public class BillsController : ControllerBase
    {
        private readonly IMediator mediator;
        private readonly ILogger<BillsController> logger;
        private readonly IMapper mapper;

        public BillsController(
            IMediator mediator,
            ILogger<BillsController> logger,
            IMapper mapper)
        {
            this.mediator = mediator;
            this.logger = logger;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult> GetBills(int customerId)
        {
            var bills = await mediator.Send(new GetBillsForCustomerQuery(customerId));
            var response = mapper.Map<List<BillReadDTO>>(bills);
            return Ok(response);
        }

        [HttpGet("{billId}")]
        public async Task<ActionResult> GetBill(int customerId, int billId)
        {
            var bill = await mediator.Send(new GetBillByIdQuery(customerId, billId));
            if (bill == null)
            {
                logger.LogWarning($"Bill {billId} not found for get");
                return NotFound();
            }

            var response = mapper.Map<BillReadDTO>(bill);
            return Ok(response);
        }



        [HttpPut("{billId}")]
        public async Task<ActionResult> UpdateBill(int customerId, int billId, BillUpdateDTO billUpdateDto)
        {
            var bill = await mediator.Send(new GetBillByIdQuery(customerId, billId));
            if (bill == null)
            {
                logger.LogWarning($"Bill {billId} not found for update");
                return NotFound();
            }

            mapper.Map(billUpdateDto, bill);
            var result = await mediator.Send(new UpdateBillCommand(bill));
            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return NoContent();
        }

        [HttpDelete("{billId}")]
        public async Task<ActionResult> DeleteBill(int customerId, int billId)
        {
            var deleted = await mediator.Send(new DeleteBillCommand(customerId, billId));
            if (!deleted)
            {
                logger.LogWarning($"Bill {billId} not found for customer {customerId} for delete");
                return NotFound();
            }

            return NoContent();
        }

        [HttpPost("/api/bills/simulate-monthly-billing")]
        public async Task<ActionResult> SimulateMonthlyBilling(
            [FromQuery] int year, 
            [FromQuery] int month, 
            [FromQuery] int? customerId = null)
        {
            var result = await mediator.Send(new SimulateMonthlyBillingCommand(year, month, customerId));
            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(result.Data);
        }
    }
}