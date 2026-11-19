using AutoMapper;
using IspAppApi.DTOs.ServicePlans;
using IspAppApi.Domain.Entities;

namespace IspAppApi.Profiles
{
    public class ServicePlanProfile : Profile
    {
        public ServicePlanProfile()
        {
            CreateMap<ServicePlan, ServicePlanReadDTO>();
            CreateMap<ServicePlanCreateDTO, ServicePlan>();
            CreateMap<ServicePlanUpdateDTO, ServicePlan>();

        }
    }
}
