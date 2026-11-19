using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IspAppApi.Domain.Entities
{
    public class Bill
    {
        public int Id { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime DueDate { get; set; }

        public decimal TotalAmount { get; set; }

        public string PaymentStatus { get; set; } = "Unpaid";

        public int CustomerId { get; set; }

        public int SubscriptionId { get; set; }

        public Customer Customer { get; set; } = null!;

        public Subscription Subscription { get; set; } = null!;

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
