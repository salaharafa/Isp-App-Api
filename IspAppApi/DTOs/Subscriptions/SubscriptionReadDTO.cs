using IspAppApi.Domain.Entities;

namespace IspAppApi.DTOs.Subscriptions
{
    public class SubscriptionReadDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public SubscriptionStatus Status { get; set; }
        public int CustomerId { get; set; }
        public int ServicePlanId { get; set; }
    }
}
