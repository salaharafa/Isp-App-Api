using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IspAppApi.Domain.Entities
{
    public class Hardware
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string SerialNumber { get; set; } = null!;

        public DateTime DateOfReceipt { get; set; }

        public DateTime? DateOfReturn { get; set; }

        public int CustomerId { get; set; }

        public Customer Customer { get; set; } = null!;
    }
}
