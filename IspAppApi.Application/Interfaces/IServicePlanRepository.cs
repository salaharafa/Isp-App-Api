using IspAppApi.Domain.Entities;

namespace IspAppApi.Application.Interfaces
{
    public interface IServicePlanRepository
    {
        Task<List<ServicePlan>> GetServicePlansAsync();
        Task<ServicePlan?> GetServicePlanAsync(int id);
        Task AddServicePlanAsync(ServicePlan plan);
        Task UpdateServicePlanAsync(ServicePlan plan);
        Task DeleteServicePlanAsync(ServicePlan plan);
    }
}
