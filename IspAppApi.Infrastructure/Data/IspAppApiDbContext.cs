using IspAppApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IspAppApi.Infrastructure.Data
{
    public class IspAppApiDbContext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<ServicePlan> ServicePlans { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Hardware> Hardwares { get; set; }
        public DbSet<Ticket> Tickets { get; set; }

        public IspAppApiDbContext(DbContextOptions<IspAppApiDbContext> options) : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Subscription>()
                .Property(s => s.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Ticket>()
                .Property(t => t.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<Bill>()
                .HasMany(b => b.Payments)
                .WithOne()
                .HasForeignKey(p => p.BillId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Bill>()
                .HasOne(b => b.Subscription)
                .WithMany(s => s.Bills)
                .HasForeignKey(b => b.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.BillId)
                .IsUnique()
                .HasFilter("[IsCancelled] = 0");

            modelBuilder.Entity<Customer>().HasData(
                new Customer
                {
                    Id = 1,
                    Name = "Ahmad Abohmid",
                    Email = "ahmad@gmail.com",
                    BillingAddress = "Aleppo - Billing",
                    ServiceAddress = "Aleppo - Service",
                    IsActive = true
                }
            );

            // Bill HasData removed: seed bills had no SubscriptionId and cannot be linked to a real subscription.

            modelBuilder.Entity<ServicePlan>().HasData(
                new ServicePlan
                {
                    Id = 1,
                    Name = "Internet 4G",
                    Speed = 10,
                    Price = 20,
                    Status = "Active",
                    IsActive = true
                },
                new ServicePlan
                {
                    Id = 2,
                    Name = "Internet 5G",
                    Speed = 50,
                    Price = 40,
                    Status = "Active",
                    IsActive = true
                });

            // Payment HasData removed: depended on seeded Bill Id=1 which has been removed.


            modelBuilder.Entity<Ticket>().HasData(
                new Ticket
                {
                    Id = 1,
                    Name = "Internet not working",
                    Status = IspAppApi.Domain.Entities.TaskStatus.New,
                    CreatedDate = new DateTime(2024, 1, 2),
                    CustomerId = 1
                }
            );
        }

    }
}
