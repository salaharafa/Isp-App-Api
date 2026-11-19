using AutoMapper;
using IspAppApi.Application.Features.Customers.Commands;
using IspAppApi.Application.Features.Customers.Queries;
using IspAppApi.Domain.Entities;
using IspAppApi.DTOs.Customers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IspAppApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [ApiVersion(1.0)]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly IMediator mediator;
        private readonly IMapper mapper;
        private readonly ILogger<CustomersController> logger;

        public CustomersController(
            IMediator mediator,
            ILogger<CustomersController> logger,
            IMapper mapper)
        {
            this.mediator = mediator;
            this.logger = logger;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult> GetCustomers()
        {
            var customers = await mediator.Send(new GetCustomersQuery());
            var response = mapper.Map<List<CustomerReadDTO>>(customers);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetCustomer(int id)
        {
            var customer = await mediator.Send(new GetCustomerByIdQuery(id));
            if (customer == null)
            {
                logger.LogWarning($"Customer {id} not found for get");
                return NotFound();
            }
            var response = mapper.Map<CustomerReadDTO>(customer);
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult> AddCustomer(CustomerCreateDTO customerCreateDto)
        {
            var customer = mapper.Map<Customer>(customerCreateDto);
            await mediator.Send(new CreateCustomerCommand(customer));
            return StatusCode(201);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateCustomer(int id, CustomerUpdateDTO customerUpdateDto)
        {
            var customer = await mediator.Send(new GetCustomerByIdQuery(id));
            if (customer == null)
            {
                logger.LogWarning($"Customer {id} not found for update");
                return NotFound();
            }
            mapper.Map(customerUpdateDto, customer);
            await mediator.Send(new UpdateCustomerCommand(customer));
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCustomer(int id)
        {
            var deleted = await mediator.Send(new DeleteCustomerCommand(id));
            if (!deleted)
            {
                logger.LogWarning($"Customer {id} not found for delete");
                return NotFound();
            }
            return NoContent();
        }
    }
}
