using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IspAppApi.Domain.Entities
{
    public class ServicePlan
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public int? Speed { get; set; }

        public decimal Price { get; set; }

        public string Status { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        public ICollection<Subscription> Subscriptions { get; set; }
            = new List<Subscription>();
    }
}
