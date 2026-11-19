using AutoMapper;
using IspAppApi.Domain.Entities;
using IspAppApi.DTOs.Subscriptions;

namespace IspAppApi.Profiles
{
    public class SubscriptionProfile : Profile
    {
        public SubscriptionProfile()
        {
            CreateMap<Subscription, SubscriptionReadDTO>();
            CreateMap<SubscriptionCreateDTO, Subscription>();
            CreateMap<SubscriptionUpdateDTO, Subscription>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CustomerId, opt => opt.Ignore())
                .ForMember(dest => dest.ServicePlanId, opt => opt.Ignore())
                .ForMember(dest => dest.StartDate, opt => opt.Ignore())
                .ForMember(dest => dest.EndDate, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore());
        }
    }
}
