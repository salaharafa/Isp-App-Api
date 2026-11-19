using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using IspAppApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IspAppApi.Infrastructure.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly IspAppApiDbContext context;

        public CustomerRepository(IspAppApiDbContext context)
        {
            this.context = context;
        }

        public async Task<List<Customer>> GetCustomersAsync()
        {
            return await context.Customers
                                .Where(c => c.IsActive)
                                .OrderBy(c => c.Name)
                                .ToListAsync();
        }

        public async Task<Customer?> GetCustomerAsync(int id)
        {
            return await context.Customers
                                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
        }

        public async Task AddCustomerAsync(Customer customer)
        {
            await context.Customers.AddAsync(customer);
            await context.SaveChangesAsync();
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            context.Customers.Update(customer);
            await context.SaveChangesAsync();
        }

        public async Task DeleteCustomerAsync(Customer customer)
        {
            customer.IsActive = false;
            context.Customers.Update(customer);
            await context.SaveChangesAsync();
        }
    }
}
