using AutoMapper;
using IspAppApi.Domain.Entities;
using IspAppApi.DTOs.Hardwares;

namespace IspAppApi.Profiles
{
    public class HardwareProfile : Profile
    {
        public HardwareProfile()
        {
            CreateMap<Hardware, HardwareReadDTO>();
            CreateMap<HardwareCreateDTO, Hardware>();
            CreateMap<HardwareUpdateDTO, Hardware>();
        }
    }
}
