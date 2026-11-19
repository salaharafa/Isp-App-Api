using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IspAppApi.Domain.Entities
{
    public class Payment
    {
        public int Id { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public decimal Amount { get; set; }

        public int BillId { get; set; }

        public bool IsCancelled { get; set; } = false;

        public string? CancellationReason { get; set; }

        public DateTime? CancelledAt { get; set; }
    }
}
