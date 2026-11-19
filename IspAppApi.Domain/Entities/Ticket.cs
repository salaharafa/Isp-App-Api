using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IspAppApi.Domain.Entities
{
    public class Ticket
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public TaskStatus Status { get; set; } = TaskStatus.New;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public int? CustomerId { get; set; }

        public Customer? Customer { get; set; }
    }

    public enum TaskStatus
    {
        New,
        InProgress,
        Closed
    }
}
