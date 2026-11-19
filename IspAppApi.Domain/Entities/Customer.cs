using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IspAppApi.Domain.Entities
{
    public class Customer
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string BillingAddress { get; set; } = null!;

        public string ServiceAddress { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        public ICollection<Bill> Bills { get; set; } = new List<Bill>();

        public ICollection<Hardware> Hardwares { get; set; } = new List<Hardware>();

        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
