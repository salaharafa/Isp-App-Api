using IspAppApi.Domain.Entities;

namespace IspAppApi.Application.Interfaces
{
    public interface ISubscriptionRepository
    {
        Task<List<Subscription>> GetSubscriptionsAsync();
        Task<Subscription?> GetSubscriptionAsync(int id);
        Task<List<Subscription>> GetSubscriptionsForCustomerAsync(int customerId);
        Task AddSubscriptionAsync(Subscription subscription);
        Task UpdateSubscriptionAsync(Subscription subscription);
    }
}
