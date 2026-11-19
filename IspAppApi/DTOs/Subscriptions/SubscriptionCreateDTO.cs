namespace IspAppApi.DTOs.Subscriptions
{
    public class SubscriptionCreateDTO
    {
        public string Name { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public int CustomerId { get; set; }
        public int ServicePlanId { get; set; }
    }
}
