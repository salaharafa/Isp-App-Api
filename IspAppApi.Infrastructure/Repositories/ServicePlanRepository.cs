using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using IspAppApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IspAppApi.Infrastructure.Repositories
{
    public class ServicePlanRepository : IServicePlanRepository
    {
        private readonly IspAppApiDbContext context;

        public ServicePlanRepository(IspAppApiDbContext context)
        {
            this.context = context;
        }

        public async Task<List<ServicePlan>> GetServicePlansAsync()
        {
            return await context.ServicePlans
                                .Where(p => p.IsActive)
                                .OrderBy(p => p.Name)
                                .ToListAsync();
        }

        public async Task<ServicePlan?> GetServicePlanAsync(int id)
        {
            return await context.ServicePlans
                                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
        }

        public async Task AddServicePlanAsync(ServicePlan plan)
        {
            plan.IsActive = true;
            await context.ServicePlans.AddAsync(plan);
            await context.SaveChangesAsync();
        }

        public async Task UpdateServicePlanAsync(ServicePlan plan)
        {
            context.ServicePlans.Update(plan);
            await context.SaveChangesAsync();
        }

        public async Task DeleteServicePlanAsync(ServicePlan plan)
        {
            context.ServicePlans.Remove(plan);
            await context.SaveChangesAsync();
        }
    }
}
