using AutoMapper;
using IspAppApi.DTOs.Bills;
using IspAppApi.Domain.Entities;

namespace IspAppApi.Profiles
{
    public class BillProfile : Profile
    {
        public BillProfile()
        {
            CreateMap<Bill, BillReadDTO>();


            CreateMap<BillUpdateDTO, Bill>()
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentStatus, opt => opt.Ignore());
        }

    }
}
