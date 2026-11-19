using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using IspAppApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IspAppApi.Infrastructure.Repositories
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly IspAppApiDbContext _context;

        public SubscriptionRepository(IspAppApiDbContext context)
        {
            _context = context;
        }

        public async Task<List<Subscription>> GetSubscriptionsAsync()
        {
            return await _context.Subscriptions
                .Include(s => s.Customer)
                .Include(s => s.ServicePlan)
                .Include(s => s.Bills)
                .ToListAsync();
        }

        public async Task<Subscription?> GetSubscriptionAsync(int id)
        {
            return await _context.Subscriptions
                .Include(s => s.Customer)
                .Include(s => s.ServicePlan)
                .Include(s => s.Bills)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<Subscription>> GetSubscriptionsForCustomerAsync(int customerId)
        {
            return await _context.Subscriptions
                .Include(s => s.ServicePlan)
                .Include(s => s.Bills)
                .Where(s => s.CustomerId == customerId)
                .ToListAsync();
        }

        public async Task AddSubscriptionAsync(Subscription subscription)
        {
            await _context.Subscriptions.AddAsync(subscription);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSubscriptionAsync(Subscription subscription)
        {
            await _context.SaveChangesAsync();
        }
    }
}
