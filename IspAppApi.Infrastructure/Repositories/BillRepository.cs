using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using IspAppApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IspAppApi.Infrastructure.Repositories
{
    public class BillRepository : IBillRepository
    {
        private readonly IspAppApiDbContext context;

        public BillRepository(IspAppApiDbContext context)
        {
            this.context = context;
        }

        public async Task<List<Bill>> GetBillsForCustomerAsync(int customerId)
        {
            return await context.Bills
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();
        }

        public async Task<Bill?> GetBillAsync(int customerId, int billId)
        {
            return await context.Bills
                .Where(b => b.CustomerId == customerId && b.Id == billId)
                .FirstOrDefaultAsync();
        }


        public async Task AddBillsRangeAsync(IEnumerable<Bill> bills)
        {
            await context.Bills.AddRangeAsync(bills);
            await context.SaveChangesAsync();
        }

        public async Task UpdateBillAsync(Bill bill)
        {
            var trackedBill = await context.Bills.FirstOrDefaultAsync(b => b.Id == bill.Id);
            if (trackedBill == null)
            {
                throw new InvalidOperationException("Bill not found.");
            }

            trackedBill.DueDate = bill.DueDate;
            // TotalAmount and PaymentStatus remain unchanged

            await context.SaveChangesAsync();
        }

        public async Task DeleteBillAsync(int customerId, int billId)
        {
            var bill = await GetBillAsync(customerId, billId);
            if (bill != null)
            {
                context.Bills.Remove(bill);
                await context.SaveChangesAsync();
            }
        }
    }
}
