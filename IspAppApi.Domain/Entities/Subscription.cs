using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IspAppApi.Domain.Entities
{
    public class Subscription
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

        public int CustomerId { get; set; }

        public int ServicePlanId { get; set; }

        public Customer Customer { get; set; } = null!;

        public ServicePlan ServicePlan { get; set; } = null!;

        public ICollection<Bill> Bills { get; set; } = new List<Bill>();
    }

    public enum SubscriptionStatus
    {
        Active,
        Suspended,
        Cancelled
    }
}
