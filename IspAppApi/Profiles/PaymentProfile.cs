using AutoMapper;
using IspAppApi.DTOs.Payments;
using IspAppApi.Domain.Entities;

namespace IspAppApi.Profiles
{
    public class PaymentProfile : Profile
    {
        public PaymentProfile()
        {
            CreateMap<Payment, PaymentReadDTO>();
            CreateMap<PaymentCreateDTO, Payment>();
        }
    }
}
