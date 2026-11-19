using AutoMapper;
using IspAppApi.Application.Features.Hardwares.Commands;
using IspAppApi.Application.Features.Hardwares.Queries;
using IspAppApi.Domain.Entities;
using IspAppApi.DTOs.Hardwares;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IspAppApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HardwaresController : ControllerBase
    {
        private readonly IMediator mediator;
        private readonly ILogger<HardwaresController> logger;
        private readonly IMapper mapper;

        public HardwaresController(
            IMediator mediator,
            ILogger<HardwaresController> logger,
            IMapper mapper)
        {
            this.mediator = mediator;
            this.logger = logger;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<HardwareReadDTO>>> GetHardwares()
        {
            var hardwares = await mediator.Send(new GetHardwaresQuery());
            var response = mapper.Map<List<HardwareReadDTO>>(hardwares);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<HardwareReadDTO>> GetHardware(int id)
        {
            var hardware = await mediator.Send(new GetHardwareByIdQuery(id));
            if (hardware == null)
            {
                logger.LogWarning($"Hardware {id} not found for get");
                return NotFound();
            }

            var response = mapper.Map<HardwareReadDTO>(hardware);
            return Ok(response);
        }

        [HttpGet("customer/{customerId}")]
        public async Task<ActionResult<List<HardwareReadDTO>>> GetHardwaresForCustomer(int customerId)
        {
            var hardwares = await mediator.Send(new GetHardwaresForCustomerQuery(customerId));
            var response = mapper.Map<List<HardwareReadDTO>>(hardwares);
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult> AddHardware(HardwareCreateDTO hardwareCreateDto)
        {
            var hardware = mapper.Map<Hardware>(hardwareCreateDto);
            await mediator.Send(new CreateHardwareCommand(hardware));
            return StatusCode(201);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateHardware(int id, HardwareUpdateDTO hardwareUpdateDto)
        {
            var hardware = await mediator.Send(new GetHardwareByIdQuery(id));
            if (hardware == null)
            {
                logger.LogWarning($"Hardware {id} not found for update");
                return NotFound();
            }

            mapper.Map(hardwareUpdateDto, hardware);
            await mediator.Send(new UpdateHardwareCommand(hardware));
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteHardware(int id)
        {
            var deleted = await mediator.Send(new DeleteHardwareCommand(id));
            if (!deleted)
            {
                logger.LogWarning($"Hardware {id} not found for delete");
                return NotFound();
            }

            return NoContent();
        }
    }
}
