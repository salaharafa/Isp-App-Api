using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using IspAppApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IspAppApi.Infrastructure.Repositories
{
    public class HardwareRepository : IHardwareRepository
    {
        private readonly IspAppApiDbContext _context;

        public HardwareRepository(IspAppApiDbContext context)
        {
            _context = context;
        }

        public async Task<List<Hardware>> GetHardwaresAsync()
        {
            return await _context.Hardwares
                .Include(h => h.Customer)
                .ToListAsync();
        }

        public async Task<Hardware?> GetHardwareAsync(int id)
        {
            return await _context.Hardwares
                .Include(h => h.Customer)
                .FirstOrDefaultAsync(h => h.Id == id);
        }

        public async Task<List<Hardware>> GetHardwaresForCustomerAsync(int customerId)
        {
            return await _context.Hardwares
                .Where(h => h.CustomerId == customerId)
                .ToListAsync();
        }

        public async Task AddHardwareAsync(Hardware hardware)
        {
            await _context.Hardwares.AddAsync(hardware);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateHardwareAsync(Hardware hardware)
        {
            _context.Hardwares.Update(hardware);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteHardwareAsync(Hardware hardware)
        {
            _context.Hardwares.Remove(hardware);
            await _context.SaveChangesAsync();
        }
    }
}
