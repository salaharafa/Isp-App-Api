using IspAppApi.Domain.Entities;

namespace IspAppApi.Application.Interfaces
{
    public interface IHardwareRepository
    {
        Task<List<Hardware>> GetHardwaresAsync();
        Task<Hardware?> GetHardwareAsync(int id);
        Task<List<Hardware>> GetHardwaresForCustomerAsync(int customerId);
        Task AddHardwareAsync(Hardware hardware);
        Task UpdateHardwareAsync(Hardware hardware);
        Task DeleteHardwareAsync(Hardware hardware);
    }
}
