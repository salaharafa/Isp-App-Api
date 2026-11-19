using AutoMapper;
using IspAppApi.Application.Features.Tickets.Commands;
using IspAppApi.Application.Features.Tickets.Queries;
using IspAppApi.Domain.Entities;
using IspAppApi.DTOs.Tickets;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IspAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [ApiVersion(1.0)]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly IMediator mediator;
        private readonly ILogger<TicketsController> logger;
        private readonly IMapper mapper;

        public TicketsController(
            IMediator mediator,
            ILogger<TicketsController> logger,
            IMapper mapper)
        {
            this.mediator = mediator;
            this.logger = logger;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult> GetTickets()
        {
            var tickets = await mediator.Send(new GetTicketsQuery());
            var response = mapper.Map<List<TicketReadDTO>>(tickets);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetTicket(int id)
        {
            var ticket = await mediator.Send(new GetTicketByIdQuery(id));
            if (ticket == null)
            {
                logger.LogWarning($"Ticket {id} not found for get");
                return NotFound();
            }

            var response = mapper.Map<TicketReadDTO>(ticket);
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult> AddTicket(TicketCreateDTO ticketCreateDto)
        {
            var ticket = mapper.Map<Ticket>(ticketCreateDto);
            ticket.CreatedDate = DateTime.UtcNow;

            await mediator.Send(new CreateTicketCommand(ticket));
            return StatusCode(201);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateTicket(int id, TicketUpdateDTO ticketUpdateDto)
        {
            var result = await mediator.Send(new UpdateTicketCommand(id, ticketUpdateDto.Name));
            if (!result.Success)
            {
                if (result.ErrorMessage == "Ticket not found.")
                {
                    logger.LogWarning($"Ticket {id} not found for update");
                    return NotFound(new { message = result.ErrorMessage });
                }

                return BadRequest(new { message = result.ErrorMessage });
            }

            return NoContent();
        }

        [HttpPut("{id}/start")]
        [HttpPut("{id}/in-progress")]
        public async Task<ActionResult> StartTicket(int id)
        {
            var result = await mediator.Send(new StartTicketCommand(id));
            if (!result.Success)
            {
                if (result.ErrorMessage == "Ticket not found.")
                {
                    logger.LogWarning($"Ticket {id} not found for start");
                    return NotFound(new { message = result.ErrorMessage });
                }

                return BadRequest(new { message = result.ErrorMessage });
            }

            return NoContent();
        }

        [HttpPut("{id}/close")]
        public async Task<ActionResult> CloseTicket(int id)
        {
            var result = await mediator.Send(new CloseTicketCommand(id));
            if (!result.Success)
            {
                if (result.ErrorMessage == "Ticket not found.")
                {
                    logger.LogWarning($"Ticket {id} not found for close");
                    return NotFound(new { message = result.ErrorMessage });
                }

                return BadRequest(new { message = result.ErrorMessage });
            }

            return NoContent();
        }

        [HttpPut("{id}/reopen")]
        public async Task<ActionResult> ReopenTicket(int id)
        {
            var result = await mediator.Send(new ReopenTicketCommand(id));
            if (!result.Success)
            {
                if (result.ErrorMessage == "Ticket not found.")
                {
                    logger.LogWarning($"Ticket {id} not found for reopen");
                    return NotFound(new { message = result.ErrorMessage });
                }

                return BadRequest(new { message = result.ErrorMessage });
            }

            return NoContent();
        }

        

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTicket(int id)
        {
            var result = await mediator.Send(new DeleteTicketCommand(id));
            if (!result.Success)
            {
                if (result.ErrorMessage == "Ticket not found.")
                {
                    logger.LogWarning($"Ticket {id} not found for delete");
                    return NotFound();
                }

                return BadRequest(new { message = result.ErrorMessage });
            }

            return NoContent();
        }
    }
}
