using IspAppApi.Domain.Entities;

namespace IspAppApi.Application.Interfaces
{
    public interface IBillRepository
    {
        Task<List<Bill>> GetBillsForCustomerAsync(int customerId);
        Task<Bill?> GetBillAsync(int customerId, int billId);
        Task AddBillsRangeAsync(IEnumerable<Bill> bills);
        Task UpdateBillAsync(Bill bill);
        Task DeleteBillAsync(int customerId, int billId);
    }
}
