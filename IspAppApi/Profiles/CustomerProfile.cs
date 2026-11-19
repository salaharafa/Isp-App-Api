using AutoMapper;
using IspAppApi.DTOs.Customers;
using IspAppApi.Domain.Entities;

namespace IspAppApi.Profiles
{
    public class CustomerProfile : Profile
    {
        public CustomerProfile()
        {
            CreateMap<Customer, CustomerReadDTO>();

            CreateMap<CustomerCreateDTO, Customer>()
                .ForMember(d => d.IsActive, s => s.MapFrom(s => true));

            CreateMap<CustomerUpdateDTO, Customer>();
        }
    }
}
