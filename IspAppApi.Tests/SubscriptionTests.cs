using AutoMapper;
using IspAppApi.Application.Common;
using IspAppApi.Application.Features.Subscriptions.Commands;
using IspAppApi.Controllers;
using IspAppApi.Domain.Entities;
using IspAppApi.DTOs.Subscriptions;
using IspAppApi.Infrastructure.Data;
using IspAppApi.Infrastructure.Repositories;
using IspAppApi.Profiles;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IspAppApi.Tests
{
    public class SubscriptionTests
    {
        private IspAppApiDbContext CreateInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<IspAppApiDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new IspAppApiDbContext(options);
        }

        private IMapper CreateAutoMapper()
        {
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<SubscriptionProfile>();
            });
            return configuration.CreateMapper();
        }

        // 1. اختبار الحالة السلبية: لا يمكن إنشاء اشتراك لعميل غير موجود
        [Fact]
        public async Task Test1_CreateSubscription_WhenCustomerDoesNotExist_ReturnsFailureAndDoesNotCreateSubscription()
        {
            using var context = CreateInMemoryDbContext(nameof(Test1_CreateSubscription_WhenCustomerDoesNotExist_ReturnsFailureAndDoesNotCreateSubscription));

            var customerRepo = new CustomerRepository(context);
            var subscriptionRepo = new SubscriptionRepository(context);
            var handler = new CreateSubscriptionCommandHandler(subscriptionRepo, customerRepo);

            var subscription = new Subscription
            {
                Name = "Home 4G",
                CustomerId = 999, // عميل غير موجود
                ServicePlanId = 1,
                StartDate = DateTime.UtcNow,
                EndDate = null
            };

            var result = await handler.Handle(new CreateSubscriptionCommand(subscription), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("999", result.ErrorMessage);
            Assert.Contains("not found", result.ErrorMessage);

            // التأكد من عدم إضافة أي اشتراك في قاعدة البيانات
            var count = await context.Subscriptions.CountAsync();
            Assert.Equal(0, count);
        }

        // 2. اختبار الحالة الإيجابية: نجاح إنشاء الاشتراك لعميل صالح وموجود
        [Fact]
        public async Task Test2_CreateSubscription_WhenCustomerExists_ReturnsSuccessAndPersistsSubscription()
        {
            using var context = CreateInMemoryDbContext(nameof(Test2_CreateSubscription_WhenCustomerExists_ReturnsSuccessAndPersistsSubscription));

            // إضافة عميل صالح ونشط
            var customer = new Customer
            {
                Id = 1,
                Name = "John Doe",
                Email = "john@example.com",
                BillingAddress = "123 Street",
                ServiceAddress = "123 Street",
                IsActive = true
            };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var customerRepo = new CustomerRepository(context);
            var subscriptionRepo = new SubscriptionRepository(context);
            var handler = new CreateSubscriptionCommandHandler(subscriptionRepo, customerRepo);

            var subscription = new Subscription
            {
                Name = "Home 5G",
                CustomerId = 1,
                ServicePlanId = 1,
                StartDate = DateTime.UtcNow,
                EndDate = null
            };

            var result = await handler.Handle(new CreateSubscriptionCommand(subscription), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
            Assert.NotNull(result.Data);

            // التأكد من حفظ الاشتراك في قاعدة البيانات بحالة نشطة وبدون تاريخ انتهاء
            var savedSubscription = await context.Subscriptions.FirstOrDefaultAsync(s => s.CustomerId == 1);
            Assert.NotNull(savedSubscription);
            Assert.Equal("Home 5G", savedSubscription.Name);
            Assert.Equal(SubscriptionStatus.Active, savedSubscription.Status);
            Assert.Null(savedSubscription.EndDate);
        }

        // 3. اختبار الحالة السلبية للعميل غير النشط (Soft-deleted): GetCustomerAsync تستبعد العملاء غير النشطين
        [Fact]
        public async Task Test3_CreateSubscription_WhenCustomerIsInactive_ReturnsFailure()
        {
            using var context = CreateInMemoryDbContext(nameof(Test3_CreateSubscription_WhenCustomerIsInactive_ReturnsFailure));

            // إضافة عميل غير نشط (IsActive = false)
            var customer = new Customer
            {
                Id = 2,
                Name = "Inactive User",
                Email = "inactive@example.com",
                BillingAddress = "Old Street",
                ServiceAddress = "Old Street",
                IsActive = false
            };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var customerRepo = new CustomerRepository(context);
            var subscriptionRepo = new SubscriptionRepository(context);
            var handler = new CreateSubscriptionCommandHandler(subscriptionRepo, customerRepo);

            var subscription = new Subscription
            {
                Name = "Fiber Plan",
                CustomerId = 2,
                ServicePlanId = 1,
                StartDate = DateTime.UtcNow,
                EndDate = null
            };

            var result = await handler.Handle(new CreateSubscriptionCommand(subscription), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("2", result.ErrorMessage);
            Assert.Contains("not found", result.ErrorMessage);

            // التأكد من عدم إنشاء الاشتراك
            Assert.Empty(context.Subscriptions);
        }

        // 4. اختبار Controller: إعادة HTTP 404 عند عدم وجود العميل
        [Fact]
        public async Task Test4_SubscriptionsController_AddSubscription_ReturnsNotFound404_WhenCustomerNotFound()
        {
            var fakeMediator = new FakeMediator(request =>
            {
                if (request is CreateSubscriptionCommand)
                {
                    return Result.Fail<Subscription>("Customer with ID 999 not found.");
                }
                throw new NotImplementedException();
            });

            var mapper = CreateAutoMapper();
            var logger = NullLogger<SubscriptionsController>.Instance;
            var controller = new SubscriptionsController(fakeMediator, logger, mapper);

            var dto = new SubscriptionCreateDTO
            {
                Name = "Test Plan",
                CustomerId = 999,
                ServicePlanId = 1,
                StartDate = DateTime.UtcNow
            };

            var actionResult = await controller.AddSubscription(dto);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        // 5. اختبار Controller: إعادة HTTP 201 عند نجاح الإنشاء
        [Fact]
        public async Task Test5_SubscriptionsController_AddSubscription_ReturnsStatusCode201_WhenSuccess()
        {
            var fakeMediator = new FakeMediator(request =>
            {
                if (request is CreateSubscriptionCommand cmd)
                {
                    Assert.Null(cmd.Subscription.EndDate);
                    return Result.Ok(cmd.Subscription);
                }
                throw new NotImplementedException();
            });

            var mapper = CreateAutoMapper();
            var logger = NullLogger<SubscriptionsController>.Instance;
            var controller = new SubscriptionsController(fakeMediator, logger, mapper);

            var dto = new SubscriptionCreateDTO
            {
                Name = "Valid Plan",
                CustomerId = 1,
                ServicePlanId = 1,
                StartDate = DateTime.UtcNow
            };

            var actionResult = await controller.AddSubscription(dto);

            var statusResult = Assert.IsType<StatusCodeResult>(actionResult);
            Assert.Equal(201, statusResult.StatusCode);
        }

        // 5b. اختبار منع تجاوز دورة الحياة: لا يستطيع العميل فرض Status أو EndDate عند الإنشاء
        [Fact]
        public async Task Test5b_CreateSubscription_IgnoresClientSuppliedStatusAndEndDate_ForcesActiveAndNullEndDate()
        {
            using var context = CreateInMemoryDbContext(nameof(Test5b_CreateSubscription_IgnoresClientSuppliedStatusAndEndDate_ForcesActiveAndNullEndDate));

            var customer = new Customer
            {
                Id = 1,
                Name = "John Doe",
                Email = "john@example.com",
                BillingAddress = "Street 1",
                ServiceAddress = "Street 1",
                IsActive = true
            };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var customerRepo = new CustomerRepository(context);
            var subscriptionRepo = new SubscriptionRepository(context);
            var handler = new CreateSubscriptionCommandHandler(subscriptionRepo, customerRepo);

            // العميل يحاول فرض حالة Cancelled وتاريخ انتهاء محدد
            var subscription = new Subscription
            {
                Name = "Tampered Plan",
                CustomerId = 1,
                ServicePlanId = 1,
                StartDate = DateTime.UtcNow,
                Status = SubscriptionStatus.Cancelled, // محاولة تجاوز
                EndDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc) // محاولة تجاوز
            };

            var result = await handler.Handle(new CreateSubscriptionCommand(subscription), CancellationToken.None);

            Assert.True(result.Success);

            var saved = await context.Subscriptions.FirstOrDefaultAsync(s => s.CustomerId == 1);
            Assert.NotNull(saved);
            // يجب أن يكون Status=Active وEndDate=null بغض النظر عما أرسله العميل
            Assert.Equal(SubscriptionStatus.Active, saved.Status);
            Assert.Null(saved.EndDate);
        }

        private void SeedTestData(IspAppApiDbContext context)
        {
            if (!context.Customers.Any(c => c.Id == 1))
            {
                context.Customers.Add(new Customer
                {
                    Id = 1,
                    Name = "John Doe",
                    Email = "john@example.com",
                    BillingAddress = "Street 1",
                    ServiceAddress = "Street 1",
                    IsActive = true
                });
            }
            if (!context.ServicePlans.Any(p => p.Id == 1))
            {
                context.ServicePlans.Add(new ServicePlan
                {
                    Id = 1,
                    Name = "Plan 1",
                    Price = 20,
                    Status = "Active",
                    IsActive = true
                });
            }
            context.SaveChanges();
        }

        // 6. اختبار التحديث العام: يسمح بتعديل Name فقط ولا يعدّل الحقول المحمية
        [Fact]
        public async Task Test6_UpdateSubscription_UpdatesName_AndDoesNotModifyProtectedFields()
        {
            using var context = CreateInMemoryDbContext(nameof(Test6_UpdateSubscription_UpdatesName_AndDoesNotModifyProtectedFields));
            SeedTestData(context);

            var originalStartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var subscription = new Subscription
            {
                Id = 10,
                Name = "Original Plan Name",
                CustomerId = 1,
                ServicePlanId = 1,
                StartDate = originalStartDate,
                EndDate = null,
                Status = SubscriptionStatus.Active
            };
            context.Subscriptions.Add(subscription);
            await context.SaveChangesAsync();

            var subscriptionRepo = new SubscriptionRepository(context);
            var handler = new UpdateSubscriptionCommandHandler(subscriptionRepo);

            var result = await handler.Handle(new UpdateSubscriptionCommand(10, "Updated Plan Name"), CancellationToken.None);

            Assert.True(result.Success);

            var updated = await context.Subscriptions.FindAsync(10);
            Assert.NotNull(updated);
            Assert.Equal("Updated Plan Name", updated.Name);
            Assert.Equal(SubscriptionStatus.Active, updated.Status);
            Assert.Null(updated.EndDate);
            Assert.Equal(1, updated.CustomerId);
            Assert.Equal(1, updated.ServicePlanId);
            Assert.Equal(originalStartDate, updated.StartDate);
        }

        // 7. اختبار الإلغاء الصريح: يحول الحالة إلى Cancelled ويسجل EndDate
        [Fact]
        public async Task Test7_CancelSubscription_SetsStatusToCancelled_AndSetsEndDate()
        {
            using var context = CreateInMemoryDbContext(nameof(Test7_CancelSubscription_SetsStatusToCancelled_AndSetsEndDate));
            SeedTestData(context);

            var subscription = new Subscription
            {
                Id = 20,
                Name = "Plan To Cancel",
                CustomerId = 1,
                ServicePlanId = 1,
                StartDate = DateTime.UtcNow.AddMonths(-1),
                EndDate = null,
                Status = SubscriptionStatus.Active
            };
            context.Subscriptions.Add(subscription);
            await context.SaveChangesAsync();

            var subscriptionRepo = new SubscriptionRepository(context);
            var handler = new CancelSubscriptionCommandHandler(subscriptionRepo);

            var result = await handler.Handle(new CancelSubscriptionCommand(20), CancellationToken.None);

            Assert.True(result.Success);

            var cancelled = await context.Subscriptions.FindAsync(20);
            Assert.NotNull(cancelled);
            Assert.Equal(SubscriptionStatus.Cancelled, cancelled.Status);
            Assert.NotNull(cancelled.EndDate);
            Assert.True(cancelled.EndDate <= DateTime.UtcNow);
        }

        // 8. اختبار رفض إلغاء اشتراك ملغى مسبقاً
        [Fact]
        public async Task Test8_CancelSubscription_WhenAlreadyCancelled_ReturnsFailure()
        {
            using var context = CreateInMemoryDbContext(nameof(Test8_CancelSubscription_WhenAlreadyCancelled_ReturnsFailure));
            SeedTestData(context);

            var subscription = new Subscription
            {
                Id = 21,
                Name = "Already Cancelled Plan",
                CustomerId = 1,
                ServicePlanId = 1,
                StartDate = DateTime.UtcNow.AddMonths(-2),
                EndDate = DateTime.UtcNow.AddMonths(-1),
                Status = SubscriptionStatus.Cancelled
            };
            context.Subscriptions.Add(subscription);
            await context.SaveChangesAsync();

            var subscriptionRepo = new SubscriptionRepository(context);
            var handler = new CancelSubscriptionCommandHandler(subscriptionRepo);

            var result = await handler.Handle(new CancelSubscriptionCommand(21), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("already cancelled", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        // 9. اختبار التعليق: يحول الحالة من Active إلى Suspended
        [Fact]
        public async Task Test9_SuspendSubscription_WhenActive_SetsStatusToSuspended()
        {
            using var context = CreateInMemoryDbContext(nameof(Test9_SuspendSubscription_WhenActive_SetsStatusToSuspended));
            SeedTestData(context);

            var subscription = new Subscription
            {
                Id = 30,
                Name = "Active Plan",
                CustomerId = 1,
                ServicePlanId = 1,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = null,
                Status = SubscriptionStatus.Active
            };
            context.Subscriptions.Add(subscription);
            await context.SaveChangesAsync();

            var subscriptionRepo = new SubscriptionRepository(context);
            var handler = new SuspendSubscriptionCommandHandler(subscriptionRepo);

            var result = await handler.Handle(new SuspendSubscriptionCommand(30), CancellationToken.None);

            Assert.True(result.Success);

            var suspended = await context.Subscriptions.FindAsync(30);
            Assert.NotNull(suspended);
            Assert.Equal(SubscriptionStatus.Suspended, suspended.Status);
        }

        // 10. اختبار رفض تعليق اشتراك معلق مسبقاً أو ملغي
        [Fact]
        public async Task Test10_SuspendSubscription_WhenSuspendedOrCancelled_ReturnsFailure()
        {
            using var context = CreateInMemoryDbContext(nameof(Test10_SuspendSubscription_WhenSuspendedOrCancelled_ReturnsFailure));
            SeedTestData(context);

            var suspendedSub = new Subscription
            {
                Id = 31,
                Name = "Suspended Plan",
                CustomerId = 1,
                ServicePlanId = 1,
                Status = SubscriptionStatus.Suspended
            };
            var cancelledSub = new Subscription
            {
                Id = 32,
                Name = "Cancelled Plan",
                CustomerId = 1,
                ServicePlanId = 1,
                Status = SubscriptionStatus.Cancelled
            };
            context.Subscriptions.AddRange(suspendedSub, cancelledSub);
            await context.SaveChangesAsync();

            var subscriptionRepo = new SubscriptionRepository(context);
            var handler = new SuspendSubscriptionCommandHandler(subscriptionRepo);

            var res1 = await handler.Handle(new SuspendSubscriptionCommand(31), CancellationToken.None);
            Assert.False(res1.Success);
            Assert.Contains("already suspended", res1.ErrorMessage, StringComparison.OrdinalIgnoreCase);

            var res2 = await handler.Handle(new SuspendSubscriptionCommand(32), CancellationToken.None);
            Assert.False(res2.Success);
            Assert.Contains("cancelled", res2.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        // 11. اختبار إعادة التفعيل: يحول الحالة من Suspended إلى Active
        [Fact]
        public async Task Test11_ResumeSubscription_WhenSuspended_SetsStatusToActive()
        {
            using var context = CreateInMemoryDbContext(nameof(Test11_ResumeSubscription_WhenSuspended_SetsStatusToActive));
            SeedTestData(context);

            var subscription = new Subscription
            {
                Id = 40,
                Name = "Suspended Plan",
                CustomerId = 1,
                ServicePlanId = 1,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = null,
                Status = SubscriptionStatus.Suspended
            };
            context.Subscriptions.Add(subscription);
            await context.SaveChangesAsync();

            var subscriptionRepo = new SubscriptionRepository(context);
            var handler = new ResumeSubscriptionCommandHandler(subscriptionRepo);

            var result = await handler.Handle(new ResumeSubscriptionCommand(40), CancellationToken.None);

            Assert.True(result.Success);

            var resumed = await context.Subscriptions.FindAsync(40);
            Assert.NotNull(resumed);
            Assert.Equal(SubscriptionStatus.Active, resumed.Status);
        }

        // 12. اختبار رفض إعادة تفعيل اشتراك نشط مسبقاً أو ملغي
        [Fact]
        public async Task Test12_ResumeSubscription_WhenActiveOrCancelled_ReturnsFailure()
        {
            using var context = CreateInMemoryDbContext(nameof(Test12_ResumeSubscription_WhenActiveOrCancelled_ReturnsFailure));
            SeedTestData(context);

            var activeSub = new Subscription
            {
                Id = 41,
                Name = "Active Plan",
                CustomerId = 1,
                ServicePlanId = 1,
                Status = SubscriptionStatus.Active
            };
            var cancelledSub = new Subscription
            {
                Id = 42,
                Name = "Cancelled Plan",
                CustomerId = 1,
                ServicePlanId = 1,
                Status = SubscriptionStatus.Cancelled
            };
            context.Subscriptions.AddRange(activeSub, cancelledSub);
            await context.SaveChangesAsync();

            var subscriptionRepo = new SubscriptionRepository(context);
            var handler = new ResumeSubscriptionCommandHandler(subscriptionRepo);

            var res1 = await handler.Handle(new ResumeSubscriptionCommand(41), CancellationToken.None);
            Assert.False(res1.Success);
            Assert.Contains("already active", res1.ErrorMessage, StringComparison.OrdinalIgnoreCase);

            var res2 = await handler.Handle(new ResumeSubscriptionCommand(42), CancellationToken.None);
            Assert.False(res2.Success);
            Assert.Contains("cancelled", res2.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        // 13. اختبار السماح بتحديث Name لاشتراك ملغي مع الحفاظ على حالته وبقية حقوله
        [Fact]
        public async Task Test13_UpdateSubscription_WhenCancelled_UpdatesNameAndPreservesCancelledStatus()
        {
            using var context = CreateInMemoryDbContext(nameof(Test13_UpdateSubscription_WhenCancelled_UpdatesNameAndPreservesCancelledStatus));
            SeedTestData(context);

            var originalEndDate = DateTime.UtcNow.AddDays(-1);
            var cancelledSub = new Subscription
            {
                Id = 50,
                Name = "Old Cancelled Name",
                CustomerId = 1,
                ServicePlanId = 1,
                Status = SubscriptionStatus.Cancelled,
                EndDate = originalEndDate
            };
            context.Subscriptions.Add(cancelledSub);
            await context.SaveChangesAsync();

            var subscriptionRepo = new SubscriptionRepository(context);
            var handler = new UpdateSubscriptionCommandHandler(subscriptionRepo);

            var result = await handler.Handle(new UpdateSubscriptionCommand(50, "New Name For Cancelled"), CancellationToken.None);

            Assert.True(result.Success);

            var updated = await context.Subscriptions.FindAsync(50);
            Assert.NotNull(updated);
            Assert.Equal("New Name For Cancelled", updated.Name);
            Assert.Equal(SubscriptionStatus.Cancelled, updated.Status);
            Assert.Equal(originalEndDate, updated.EndDate);
            Assert.Equal(1, updated.CustomerId);
            Assert.Equal(1, updated.ServicePlanId);
        }

        // 14. اختبار نجاح إلغاء اشتراك حالته Suspended وتسجيل EndDate
        [Fact]
        public async Task Test14_CancelSubscription_WhenSuspended_SetsStatusToCancelled_AndSetsEndDate()
        {
            using var context = CreateInMemoryDbContext(nameof(Test14_CancelSubscription_WhenSuspended_SetsStatusToCancelled_AndSetsEndDate));
            SeedTestData(context);

            var suspendedSub = new Subscription
            {
                Id = 60,
                Name = "Suspended Plan To Cancel",
                CustomerId = 1,
                ServicePlanId = 1,
                StartDate = DateTime.UtcNow.AddMonths(-1),
                EndDate = null,
                Status = SubscriptionStatus.Suspended
            };
            context.Subscriptions.Add(suspendedSub);
            await context.SaveChangesAsync();

            var subscriptionRepo = new SubscriptionRepository(context);
            var handler = new CancelSubscriptionCommandHandler(subscriptionRepo);

            var result = await handler.Handle(new CancelSubscriptionCommand(60), CancellationToken.None);

            Assert.True(result.Success);

            var cancelled = await context.Subscriptions.FindAsync(60);
            Assert.NotNull(cancelled);
            Assert.Equal(SubscriptionStatus.Cancelled, cancelled.Status);
            Assert.NotNull(cancelled.EndDate);
            Assert.True(cancelled.EndDate <= DateTime.UtcNow);
        }

        private class FakeMediator : IMediator
        {
            private readonly Func<object, object> _handler;

            public FakeMediator(Func<object, object> handler)
            {
                _handler = handler;
            }

            public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            {
                var result = _handler(request);
                return Task.FromResult((TResponse)result);
            }

            public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            {
                _handler(request);
                return Task.CompletedTask;
            }

            public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<object?>(_handler(request));
            }

            public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;

            public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        }
    }
}
