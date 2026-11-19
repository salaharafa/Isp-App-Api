using AutoMapper;
using IspAppApi.DTOs.Tickets;
using IspAppApi.Domain.Entities;

namespace IspAppApi.Profiles
{
    public class TicketProfile : Profile
    {
        public TicketProfile()
        {
            CreateMap<Ticket, TicketReadDTO>();
            CreateMap<TicketCreateDTO, Ticket>();
            CreateMap<TicketUpdateDTO, Ticket>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CustomerId, opt => opt.Ignore())
                .ForMember(dest => dest.Customer, opt => opt.Ignore());
        }
    }
}
