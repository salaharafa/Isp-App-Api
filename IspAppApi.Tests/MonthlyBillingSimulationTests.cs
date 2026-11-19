using IspAppApi.Application.Features.Bills.Commands;
using IspAppApi.Domain.Entities;
using IspAppApi.Infrastructure.Data;
using IspAppApi.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IspAppApi.Tests
{
    public class MonthlyBillingSimulationTests
    {
        private IspAppApiDbContext CreateInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<IspAppApiDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new IspAppApiDbContext(options);
        }

        private async Task<(Customer customer, ServicePlan plan)> SeedTestCustomerAndPlanAsync(IspAppApiDbContext context, decimal planPrice = 30m)
        {
            var customer = new Customer
            {
                Id = 1,
                Name = "Ahmad Test",
                Email = "ahmad@test.com",
                BillingAddress = "Aleppo",
                ServiceAddress = "Aleppo",
                IsActive = true
            };

            var plan = new ServicePlan
            {
                Id = 1,
                Name = "Plan 50M",
                Speed = 50,
                Price = planPrice,
                Status = "Active",
                IsActive = true
            };

            context.Customers.Add(customer);
            context.ServicePlans.Add(plan);
            await context.SaveChangesAsync();

            return (customer, plan);
        }

        // 1. صحة المبلغ والربط: يتم إصدار الفاتورة بسعر الباقة وربطها بالاشتراك والعميل الصحيحين
        [Fact]
        public async Task Test1_SimulateMonthlyBilling_ActiveSubscription_CreatesBillWithCorrectAmountAndLinks()
        {
            using var context = CreateInMemoryDbContext(nameof(Test1_SimulateMonthlyBilling_ActiveSubscription_CreatesBillWithCorrectAmountAndLinks));
            var (customer, plan) = await SeedTestCustomerAndPlanAsync(context, planPrice: 45m);

            var subscription = new Subscription
            {
                Id = 10,
                Name = "Fiber Home",
                CustomerId = customer.Id,
                ServicePlanId = plan.Id,
                Status = SubscriptionStatus.Active,
                StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };
            context.Subscriptions.Add(subscription);
            await context.SaveChangesAsync();

            var subRepo = new SubscriptionRepository(context);
            var billRepo = new BillRepository(context);
            var handler = new SimulateMonthlyBillingCommandHandler(subRepo, billRepo);

            var command = new SimulateMonthlyBillingCommand(2026, 11);
            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(1, result.Data!.BillsGeneratedCount);
            Assert.Equal(0, result.Data.SubscriptionsSuspendedCount);

            var bill = await context.Bills.FirstOrDefaultAsync(b => b.SubscriptionId == 10);
            Assert.NotNull(bill);
            Assert.Equal(customer.Id, bill.CustomerId);
            Assert.Equal(10, bill.SubscriptionId);
            Assert.Equal(45m, bill.TotalAmount);
            Assert.Equal("Unpaid", bill.PaymentStatus);
            Assert.Equal(new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc), bill.CreatedDate);

            // بقاء الاشتراك نشطاً
            var updatedSub = await context.Subscriptions.FindAsync(10);
            Assert.NotNull(updatedSub);
            Assert.Equal(SubscriptionStatus.Active, updatedSub.Status);
        }

        // 2. منع التكرار: تشغيل العملية أكثر من مرة لنفس الشهر لا ينشئ فواتير مكررة
        [Fact]
        public async Task Test2_SimulateMonthlyBilling_RunTwice_PreventsDuplicateBills()
        {
            using var context = CreateInMemoryDbContext(nameof(Test2_SimulateMonthlyBilling_RunTwice_PreventsDuplicateBills));
            var (customer, plan) = await SeedTestCustomerAndPlanAsync(context, planPrice: 20m);

            var subscription = new Subscription
            {
                Id = 20,
                Name = "DSL Home",
                CustomerId = customer.Id,
                ServicePlanId = plan.Id,
                Status = SubscriptionStatus.Active,
                StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };
            context.Subscriptions.Add(subscription);
            await context.SaveChangesAsync();

            var subRepo = new SubscriptionRepository(context);
            var billRepo = new BillRepository(context);
            var handler = new SimulateMonthlyBillingCommandHandler(subRepo, billRepo);

            // التشغيل الأول لشهر 11
            var firstRun = await handler.Handle(new SimulateMonthlyBillingCommand(2026, 11), CancellationToken.None);
            Assert.True(firstRun.Success);
            Assert.Equal(1, firstRun.Data!.BillsGeneratedCount);

            // التشغيل الثاني لنفس الشهر 11
            var secondRun = await handler.Handle(new SimulateMonthlyBillingCommand(2026, 11), CancellationToken.None);
            Assert.True(secondRun.Success);
            Assert.Equal(0, secondRun.Data!.BillsGeneratedCount);
            Assert.Equal(1, secondRun.Data.SkippedSubscriptionsCount);

            // التأكد من وجود فاتورة واحدة فقط في قاعدة البيانات
            var billsCount = await context.Bills.CountAsync(b => b.SubscriptionId == 20);
            Assert.Equal(1, billsCount);
        }

        // 3. دين غير مدفوع: إذا كان للاشتراك فاتورة سابقة غير مدفوعة، يتم تعليق الاشتراك ولا تُصدر فاتورة جديدة
        [Fact]
        public async Task Test3_SimulateMonthlyBilling_UnpaidPastDebt_SuspendsSubscriptionAndSkipsBilling()
        {
            using var context = CreateInMemoryDbContext(nameof(Test3_SimulateMonthlyBilling_UnpaidPastDebt_SuspendsSubscriptionAndSkipsBilling));
            var (customer, plan) = await SeedTestCustomerAndPlanAsync(context);

            var subscription = new Subscription
            {
                Id = 30,
                Name = "Office 4G",
                CustomerId = customer.Id,
                ServicePlanId = plan.Id,
                Status = SubscriptionStatus.Active,
                StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };
            context.Subscriptions.Add(subscription);

            // فاتورة سابقة لشهر 10 غير مسددة
            var unpaidPastBill = new Bill
            {
                Id = 301,
                CustomerId = customer.Id,
                SubscriptionId = 30,
                TotalAmount = 30m,
                PaymentStatus = "Unpaid",
                CreatedDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                DueDate = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc)
            };
            context.Bills.Add(unpaidPastBill);
            await context.SaveChangesAsync();

            var subRepo = new SubscriptionRepository(context);
            var billRepo = new BillRepository(context);
            var handler = new SimulateMonthlyBillingCommandHandler(subRepo, billRepo);

            // محاكاة شهر 11
            var result = await handler.Handle(new SimulateMonthlyBillingCommand(2026, 11), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data!.BillsGeneratedCount);
            Assert.Equal(1, result.Data.SubscriptionsSuspendedCount);
            Assert.Contains(30, result.Data.SuspendedSubscriptionIds);

            // التحقق من تعليق الاشتراك وعدم إنشاء فاتورة جديدة لشهر 11
            var updatedSub = await context.Subscriptions.FindAsync(30);
            Assert.NotNull(updatedSub);
            Assert.Equal(SubscriptionStatus.Suspended, updatedSub.Status);

            var bills = await context.Bills.Where(b => b.SubscriptionId == 30).ToListAsync();
            Assert.Single(bills);
            Assert.Equal(301, bills[0].Id);
        }

        // 4. عدم تعليق اشتراكات العميل الأخرى: إذا كان للعميل اشتراك متعثر وآخر سليم، يُعلّق المتعثر فقط ويُفوتر السليم
        [Fact]
        public async Task Test4_SimulateMonthlyBilling_MultipleSubscriptions_OnlySuspendsIndebtedSubscription()
        {
            using var context = CreateInMemoryDbContext(nameof(Test4_SimulateMonthlyBilling_MultipleSubscriptions_OnlySuspendsIndebtedSubscription));
            var (customer, plan) = await SeedTestCustomerAndPlanAsync(context);

            // اشتراك أول عليه دين سابق
            var subWithDebt = new Subscription
            {
                Id = 41,
                Name = "Home Internet",
                CustomerId = customer.Id,
                ServicePlanId = plan.Id,
                Status = SubscriptionStatus.Active,
                StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };
            // اشتراك ثانٍ لنفس العميل سليم (فاتورته السابقة مسددة)
            var subClean = new Subscription
            {
                Id = 42,
                Name = "Shop Internet",
                CustomerId = customer.Id,
                ServicePlanId = plan.Id,
                Status = SubscriptionStatus.Active,
                StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            context.Subscriptions.AddRange(subWithDebt, subClean);

            // فاتورة غير مسددة للاشتراك 41 لشهر 10
            context.Bills.Add(new Bill
            {
                Id = 401,
                CustomerId = customer.Id,
                SubscriptionId = 41,
                TotalAmount = 30m,
                PaymentStatus = "Unpaid",
                CreatedDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                DueDate = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc)
            });

            // فاتورة مسددة للاشتراك 42 لشهر 10
            context.Bills.Add(new Bill
            {
                Id = 402,
                CustomerId = customer.Id,
                SubscriptionId = 42,
                TotalAmount = 30m,
                PaymentStatus = "Paid",
                CreatedDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                DueDate = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc)
            });

            await context.SaveChangesAsync();

            var subRepo = new SubscriptionRepository(context);
            var billRepo = new BillRepository(context);
            var handler = new SimulateMonthlyBillingCommandHandler(subRepo, billRepo);

            // محاكاة شهر 11
            var result = await handler.Handle(new SimulateMonthlyBillingCommand(2026, 11), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(1, result.Data!.BillsGeneratedCount);
            Assert.Equal(1, result.Data.SubscriptionsSuspendedCount);

            // التحقق من أن الاشتراك المتعثر 41 عُلّق ولم تصدر له فاتورة لشهر 11
            var sub41 = await context.Subscriptions.FindAsync(41);
            Assert.Equal(SubscriptionStatus.Suspended, sub41!.Status);
            Assert.Equal(1, await context.Bills.CountAsync(b => b.SubscriptionId == 41));

            // التحقق من أن الاشتراك السليم 42 بقي نشطاً وأُصدرت له فاتورة شهر 11
            var sub42 = await context.Subscriptions.FindAsync(42);
            Assert.Equal(SubscriptionStatus.Active, sub42!.Status);
            var sub42Bills = await context.Bills.Where(b => b.SubscriptionId == 42).ToListAsync();
            Assert.Equal(2, sub42Bills.Count);
            Assert.Contains(sub42Bills, b => b.CreatedDate.Month == 11 && b.CreatedDate.Year == 2026);
        }

        // 5. الاشتراك الملغى نهائي: لا تُصدر له فواتير ولا يتغير وضعه
        [Fact]
        public async Task Test5_SimulateMonthlyBilling_CancelledSubscription_IsSkipped()
        {
            using var context = CreateInMemoryDbContext(nameof(Test5_SimulateMonthlyBilling_CancelledSubscription_IsSkipped));
            var (customer, plan) = await SeedTestCustomerAndPlanAsync(context);

            var cancelledSub = new Subscription
            {
                Id = 50,
                Name = "Old Service",
                CustomerId = customer.Id,
                ServicePlanId = plan.Id,
                Status = SubscriptionStatus.Cancelled,
                StartDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc)
            };
            context.Subscriptions.Add(cancelledSub);
            await context.SaveChangesAsync();

            var subRepo = new SubscriptionRepository(context);
            var billRepo = new BillRepository(context);
            var handler = new SimulateMonthlyBillingCommandHandler(subRepo, billRepo);

            var result = await handler.Handle(new SimulateMonthlyBillingCommand(2026, 11), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data!.BillsGeneratedCount);
            Assert.Equal(1, result.Data.SkippedSubscriptionsCount);

            var sub = await context.Subscriptions.FindAsync(50);
            Assert.Equal(SubscriptionStatus.Cancelled, sub!.Status);
            Assert.Empty(context.Bills.Where(b => b.SubscriptionId == 50));
        }

        // 6. تغيير سعر الباقة لا يغيّر مبالغ الفواتير التاريخية
        [Fact]
        public async Task Test6_SimulateMonthlyBilling_PlanPriceChange_DoesNotAffectHistoricalBills()
        {
            using var context = CreateInMemoryDbContext(nameof(Test6_SimulateMonthlyBilling_PlanPriceChange_DoesNotAffectHistoricalBills));
            var (customer, plan) = await SeedTestCustomerAndPlanAsync(context, planPrice: 20m);

            var subscription = new Subscription
            {
                Id = 60,
                Name = "Home 4G",
                CustomerId = customer.Id,
                ServicePlanId = plan.Id,
                Status = SubscriptionStatus.Active,
                StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };
            context.Subscriptions.Add(subscription);

            // فاتورة سابقة لشهر 10 بالسعر القديم (20m) ومسددة
            var historicalBill = new Bill
            {
                Id = 601,
                CustomerId = customer.Id,
                SubscriptionId = 60,
                TotalAmount = 20m,
                PaymentStatus = "Paid",
                CreatedDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                DueDate = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc)
            };
            context.Bills.Add(historicalBill);

            // تحديث سعر الباقة إلى 35m
            plan.Price = 35m;
            await context.SaveChangesAsync();

            var subRepo = new SubscriptionRepository(context);
            var billRepo = new BillRepository(context);
            var handler = new SimulateMonthlyBillingCommandHandler(subRepo, billRepo);

            // محاكاة شهر 11
            var result = await handler.Handle(new SimulateMonthlyBillingCommand(2026, 11), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(1, result.Data!.BillsGeneratedCount);

            // الفاتورة التاريخية لشهر 10 بقيت بمبلغها الأصلي 20m
            var oldBill = await context.Bills.FindAsync(601);
            Assert.NotNull(oldBill);
            Assert.Equal(20m, oldBill.TotalAmount);

            // الفاتورة الجديدة لشهر 11 صدرت بالسعر الجديد 35m
            var newBill = await context.Bills.FirstOrDefaultAsync(b => b.SubscriptionId == 60 && b.CreatedDate.Month == 11);
            Assert.NotNull(newBill);
            Assert.Equal(35m, newBill.TotalAmount);
        }
    }
}
