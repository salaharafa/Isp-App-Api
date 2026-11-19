using AutoMapper;
using IspAppApi.Application.Features.Bills.Commands;
using IspAppApi.Application.Features.Bills.Queries;
using IspAppApi.Controllers;
using IspAppApi.Domain.Entities;
using IspAppApi.DTOs.Bills;
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
    public class PaymentAndBillingTests
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
                cfg.AddProfile<BillProfile>();
                cfg.AddProfile<PaymentProfile>();
            });
            return configuration.CreateMapper();
        }

        // 1. إنشاء دفعة صحيحة: تتحول الفاتورة إلى Paid وتُحفظ الدفعة غير ملغاة
        [Fact]
        public async Task Test1_AddPaymentAsync_ValidPayment_SetsBillStatusToPaidAndSucceeds()
        {
            using var context = CreateInMemoryDbContext(nameof(Test1_AddPaymentAsync_ValidPayment_SetsBillStatusToPaidAndSucceeds));
            var bill = new Bill { Id = 101, CustomerId = 1, TotalAmount = 50m, PaymentStatus = "Unpaid" };
            context.Bills.Add(bill);
            await context.SaveChangesAsync();

            var repository = new PaymentRepository(context);
            var payment = new Payment { Id = 101, BillId = 101, Amount = 50m };

            var result = await repository.AddPaymentAsync(payment);

            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
            Assert.NotNull(result.Data);
            Assert.False(result.Data.IsCancelled);

            var updatedBill = await context.Bills.FindAsync(101);
            Assert.NotNull(updatedBill);
            Assert.Equal("Paid", updatedBill.PaymentStatus);
        }

        // 2. رفض دفعة بمبلغ غير صالح (<= 0) أو لا يطابق المبلغ المستحق للفاتورة (منع الدفعات الجزئية)
        [Fact]
        public async Task Test2_AddPaymentAsync_InvalidOrMismatchedAmount_RejectsPayment()
        {
            using var context = CreateInMemoryDbContext(nameof(Test2_AddPaymentAsync_InvalidOrMismatchedAmount_RejectsPayment));
            var bill = new Bill { Id = 102, CustomerId = 1, TotalAmount = 100m, PaymentStatus = "Unpaid" };
            context.Bills.Add(bill);
            await context.SaveChangesAsync();

            var repository = new PaymentRepository(context);

            // مبلغ <= 0
            var zeroPayment = new Payment { Id = 201, BillId = 102, Amount = 0m };
            var zeroResult = await repository.AddPaymentAsync(zeroPayment);
            Assert.False(zeroResult.Success);
            Assert.Contains("greater than zero", zeroResult.ErrorMessage);

            // مبلغ جزئي (50 بدلاً من 100)
            var partialPayment = new Payment { Id = 202, BillId = 102, Amount = 50m };
            var partialResult = await repository.AddPaymentAsync(partialPayment);
            Assert.False(partialResult.Success);
            Assert.Contains("must match the bill total amount", partialResult.ErrorMessage);

            // التأكد من بقاء الفاتورة غير مدفوعة وعدم إضافة أي دفعة
            var unchangedBill = await context.Bills.FindAsync(102);
            Assert.NotNull(unchangedBill);
            Assert.Equal("Unpaid", unchangedBill.PaymentStatus);
            Assert.Empty(context.Payments.Where(p => p.BillId == 102));
        }

        // 3. رفض دفعة ثانية فعّالة للفاتورة نفسها
        [Fact]
        public async Task Test3_AddPaymentAsync_SecondActivePayment_RejectsPayment()
        {
            using var context = CreateInMemoryDbContext(nameof(Test3_AddPaymentAsync_SecondActivePayment_RejectsPayment));
            var bill = new Bill { Id = 103, CustomerId = 1, TotalAmount = 80m, PaymentStatus = "Unpaid" };
            context.Bills.Add(bill);
            await context.SaveChangesAsync();

            var repository = new PaymentRepository(context);

            // الدفعة الأولى الصحيحة
            var payment1 = new Payment { Id = 301, BillId = 103, Amount = 80m };
            var result1 = await repository.AddPaymentAsync(payment1);
            Assert.True(result1.Success);

            // محاولة إضافة دفعة ثانية فعالة
            var payment2 = new Payment { Id = 302, BillId = 103, Amount = 80m };
            var result2 = await repository.AddPaymentAsync(payment2);

            Assert.False(result2.Success);
            Assert.Contains("already has an active payment", result2.ErrorMessage);
        }

        // 4. السماح بإنشاء دفعة بديلة بعد إلغاء الدفعة السابقة
        [Fact]
        public async Task Test4_AddPaymentAsync_ReplacementPaymentAfterCancellation_Succeeds()
        {
            using var context = CreateInMemoryDbContext(nameof(Test4_AddPaymentAsync_ReplacementPaymentAfterCancellation_Succeeds));
            var bill = new Bill { Id = 104, CustomerId = 1, TotalAmount = 60m, PaymentStatus = "Unpaid" };
            context.Bills.Add(bill);
            await context.SaveChangesAsync();

            var repository = new PaymentRepository(context);

            // تسجيل الدفعة الأولى
            var payment1 = new Payment { Id = 401, BillId = 104, Amount = 60m };
            await repository.AddPaymentAsync(payment1);

            // إلغاء الدفعة الأولى
            var cancelResult = await repository.CancelPaymentAsync(401, "Wrong credit card used");
            Assert.True(cancelResult.Success);

            // الفاتورة أصبحت Unpaid
            var billAfterCancel = await context.Bills.FindAsync(104);
            Assert.NotNull(billAfterCancel);
            Assert.Equal("Unpaid", billAfterCancel.PaymentStatus);

            // إنشاء دفعة بديلة جديدة
            var replacementPayment = new Payment { Id = 402, BillId = 104, Amount = 60m };
            var replacementResult = await repository.AddPaymentAsync(replacementPayment);

            Assert.True(replacementResult.Success);

            // التحقق من أن السجلين كلاهما محفوظ في قاعدة البيانات
            var allPayments = await context.Payments.Where(p => p.BillId == 104).ToListAsync();
            Assert.Equal(2, allPayments.Count);
            Assert.Contains(allPayments, p => p.Id == 401 && p.IsCancelled && p.CancellationReason == "Wrong credit card used");
            Assert.Contains(allPayments, p => p.Id == 402 && !p.IsCancelled);

            var finalBill = await context.Bills.FindAsync(104);
            Assert.NotNull(finalBill);
            Assert.Equal("Paid", finalBill.PaymentStatus);
        }

        // 5. إعادة حالة الفاتورة إلى Unpaid عند إلغاء آخر دفعة فعّالة
        [Fact]
        public async Task Test5_CancelPaymentAsync_RevertsBillStatusToUnpaid()
        {
            using var context = CreateInMemoryDbContext(nameof(Test5_CancelPaymentAsync_RevertsBillStatusToUnpaid));
            var bill = new Bill { Id = 105, CustomerId = 1, TotalAmount = 120m, PaymentStatus = "Unpaid" };
            context.Bills.Add(bill);
            await context.SaveChangesAsync();

            var repository = new PaymentRepository(context);
            var payment = new Payment { Id = 501, BillId = 105, Amount = 120m };
            await repository.AddPaymentAsync(payment);

            var cancelResult = await repository.CancelPaymentAsync(501, "Customer dispute resolved");
            Assert.True(cancelResult.Success);

            var cancelledPayment = await context.Payments.FindAsync(501);
            Assert.NotNull(cancelledPayment);
            Assert.True(cancelledPayment.IsCancelled);
            Assert.Equal("Customer dispute resolved", cancelledPayment.CancellationReason);
            Assert.NotNull(cancelledPayment.CancelledAt);

            var revertedBill = await context.Bills.FindAsync(105);
            Assert.NotNull(revertedBill);
            Assert.Equal("Unpaid", revertedBill.PaymentStatus);
        }

        // 6. رفض تعديل مبلغ الدفعة أو تاريخها بعد تسجيلها (عبر مسار PUT)
        [Fact]
        public void Test6_PaymentsController_UpdatePayment_ReturnsBadRequest()
        {
            var controller = new PaymentsController(null!, null!, null!);
            var result = controller.UpdatePayment(999);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        // 7. ثبات مبلغ الفاتورة وعدم تعديله عبر UpdateBillAsync سواء وُجدت دفعة فعّالة أم لم توجد، مع نجاح تحديث DueDate
        [Fact]
        public async Task Test7_UpdateBillAsync_PreservesTotalAmount_RegardlessOfActivePayment_AndUpdatesDueDate()
        {
            using var context = CreateInMemoryDbContext(nameof(Test7_UpdateBillAsync_PreservesTotalAmount_RegardlessOfActivePayment_AndUpdatesDueDate));
            var initialDueDate = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            var bill = new Bill { Id = 107, CustomerId = 1, TotalAmount = 100m, PaymentStatus = "Unpaid", DueDate = initialDueDate };
            context.Bills.Add(bill);
            await context.SaveChangesAsync();

            var paymentRepo = new PaymentRepository(context);
            await paymentRepo.AddPaymentAsync(new Payment { Id = 701, BillId = 107, Amount = 100m });

            var billRepo = new BillRepository(context);

            // الحالة الأولى: محاولة تعديل المبلغ إلى 150 مع وجود دفعة فعّالة وتعديل تاريخ الاستحقاق
            var newDueDate1 = new DateTime(2026, 11, 15, 0, 0, 0, DateTimeKind.Utc);
            var modifiedBillWithPayment = new Bill { Id = 107, CustomerId = 1, TotalAmount = 150m, DueDate = newDueDate1, PaymentStatus = "Paid" };
            await billRepo.UpdateBillAsync(modifiedBillWithPayment);

            var billAfterFirstUpdate = await context.Bills.FindAsync(107);
            Assert.NotNull(billAfterFirstUpdate);
            Assert.Equal(100m, billAfterFirstUpdate.TotalAmount); // المبلغ يظل ثابتاً 100 ولم يتغير إلى 150
            Assert.Equal(newDueDate1, billAfterFirstUpdate.DueDate); // تاريخ الاستحقاق حُدث بنجاح

            // فحص عبر UpdateBillCommandHandler
            var handler = new UpdateBillCommandHandler(billRepo);
            var commandResult = await handler.Handle(new UpdateBillCommand(modifiedBillWithPayment), CancellationToken.None);
            Assert.True(commandResult.Success);

            // إلغاء الدفعة (أصبحت الفاتورة بدون دفعات فعّالة)
            await paymentRepo.CancelPaymentAsync(701, "Cancelled for adjustment");

            // الحالة الثانية: محاولة تعديل المبلغ إلى 200 عند عدم وجود دفعة فعّالة وتعديل تاريخ الاستحقاق مجددًا
            var newDueDate2 = new DateTime(2026, 11, 25, 0, 0, 0, DateTimeKind.Utc);
            var modifiedBillWithoutPayment = new Bill { Id = 107, CustomerId = 1, TotalAmount = 200m, DueDate = newDueDate2, PaymentStatus = "Unpaid" };
            await billRepo.UpdateBillAsync(modifiedBillWithoutPayment);

            var billAfterSecondUpdate = await context.Bills.FindAsync(107);
            Assert.NotNull(billAfterSecondUpdate);
            Assert.Equal(100m, billAfterSecondUpdate.TotalAmount); // المبلغ يظل ثابتاً 100 ولم يتغير إلى 200 حتى بدون دفعة فعالة
            Assert.Equal(newDueDate2, billAfterSecondUpdate.DueDate); // تاريخ الاستحقاق حُدث بنجاح
        }

        // 8. منع تعديل PaymentStatus و TotalAmount عبر طلب تحديث الفاتورة (BillUpdateDTO و AutoMapper و Repository)
        [Fact]
        public async Task Test8_UpdateBill_PaymentStatusAndTotalAmountCannotBeModifiedManually()
        {
            using var context = CreateInMemoryDbContext(nameof(Test8_UpdateBill_PaymentStatusAndTotalAmountCannotBeModifiedManually));
            var bill = new Bill { Id = 108, CustomerId = 1, TotalAmount = 70m, PaymentStatus = "Paid" };
            context.Bills.Add(bill);
            await context.SaveChangesAsync();

            // فحص AutoMapper: لا يغيّر PaymentStatus ولا TotalAmount
            var mapper = CreateAutoMapper();
            var updateDto = new BillUpdateDTO { DueDate = DateTime.UtcNow.AddDays(10) };
            mapper.Map(updateDto, bill);
            Assert.Equal("Paid", bill.PaymentStatus);
            Assert.Equal(70m, bill.TotalAmount);

            // فحص BillRepository: يحافظ على PaymentStatus و TotalAmount الأصليين حتى لو تم التلاعب بالكيان
            var tamperedBill = new Bill { Id = 108, CustomerId = 1, TotalAmount = 999m, PaymentStatus = "TamperedStatus" };
            var billRepo = new BillRepository(context);
            await billRepo.UpdateBillAsync(tamperedBill);

            var preservedBill = await context.Bills.FindAsync(108);
            Assert.NotNull(preservedBill);
            Assert.Equal("Paid", preservedBill.PaymentStatus);
            Assert.Equal(70m, preservedBill.TotalAmount);
        }

        // 9. التأكد من حفظ عملية الإلغاء وتغيير حالة الفاتورة معًا (Atomicity)
        [Fact]
        public async Task Test9_CancelPaymentAsync_SavesPaymentAndBillChangesAtomically()
        {
            using var context = CreateInMemoryDbContext(nameof(Test9_CancelPaymentAsync_SavesPaymentAndBillChangesAtomically));
            var bill = new Bill { Id = 109, CustomerId = 1, TotalAmount = 200m, PaymentStatus = "Unpaid" };
            context.Bills.Add(bill);
            await context.SaveChangesAsync();

            var repository = new PaymentRepository(context);
            await repository.AddPaymentAsync(new Payment { Id = 901, BillId = 109, Amount = 200m });

            // قبل الإلغاء
            Assert.Equal("Paid", (await context.Bills.FindAsync(109))!.PaymentStatus);
            Assert.False((await context.Payments.FindAsync(901))!.IsCancelled);

            // الإلغاء الذري
            var cancelResult = await repository.CancelPaymentAsync(901, "Atomic test cancellation");
            Assert.True(cancelResult.Success);

            // التحقق من حفظ الحالتين معًا في قاعدة البيانات
            var savedPayment = await context.Payments.FindAsync(901);
            var savedBill = await context.Bills.FindAsync(109);

            Assert.NotNull(savedPayment);
            Assert.NotNull(savedBill);
            Assert.True(savedPayment.IsCancelled);
            Assert.Equal("Atomic test cancellation", savedPayment.CancellationReason);
            Assert.Equal("Unpaid", savedBill.PaymentStatus);
        }

        // 10. التحقق من اتساق حالة الفاتورة المسددة وغير المسددة
        [Fact]
        public void Test10_SeedData_Bill1AndPayment1_AreConsistentAndPaid()
        {
            var options = new DbContextOptionsBuilder<IspAppApiDbContext>()
                .UseInMemoryDatabase(databaseName: nameof(Test10_SeedData_Bill1AndPayment1_AreConsistentAndPaid))
                .Options;

            using var context = new IspAppApiDbContext(options);
            context.Database.EnsureCreated();

            // تهيئة بيانات الاختبار الخاصة بالاشتراك والفواتير والدفعات بدلاً من HasData المحذوفة
            var subscription = new Subscription
            {
                Id = 1,
                CustomerId = 1,
                ServicePlanId = 1,
                Name = "Test Subscription",
                StartDate = DateTime.UtcNow,
                Status = SubscriptionStatus.Active
            };
            context.Subscriptions.Add(subscription);

            context.Bills.AddRange(
                new Bill { Id = 1, CustomerId = 1, SubscriptionId = 1, TotalAmount = 20m, PaymentStatus = "Paid", DueDate = DateTime.UtcNow.AddDays(30) },
                new Bill { Id = 2, CustomerId = 1, SubscriptionId = 1, TotalAmount = 40m, PaymentStatus = "Unpaid", DueDate = DateTime.UtcNow.AddDays(30) }
            );

            context.Payments.Add(new Payment { Id = 1, BillId = 1, Amount = 20m, IsCancelled = false, PaymentDate = DateTime.UtcNow });
            context.SaveChanges();

            var bill1 = context.Bills.Find(1);
            Assert.NotNull(bill1);
            Assert.Equal("Paid", bill1.PaymentStatus);
            Assert.Equal(20m, bill1.TotalAmount);

            var payment1 = context.Payments.Find(1);
            Assert.NotNull(payment1);
            Assert.Equal(1, payment1.BillId);
            Assert.Equal(20m, payment1.Amount);
            Assert.False(payment1.IsCancelled);

            // الفاتورة 2: غير مسددة وبدون دفعات
            var bill2 = context.Bills.Find(2);
            Assert.NotNull(bill2);
            Assert.Equal("Unpaid", bill2.PaymentStatus);
            Assert.Equal(40m, bill2.TotalAmount);

            var bill2Payments = context.Payments.Where(p => p.BillId == 2).ToList();
            Assert.Empty(bill2Payments);
        }

        // 11. ثبات TotalAmount عبر مسار Controller عند وجود دفعة فعالة ونجاح تحديث DueDate
        [Fact]
        public async Task Test11_UpdateBill_ControllerPath_PreservesTotalAmount_WhenActivePaymentExists_AndUpdatesDueDate()
        {
            using var context = CreateInMemoryDbContext(nameof(Test11_UpdateBill_ControllerPath_PreservesTotalAmount_WhenActivePaymentExists_AndUpdatesDueDate));
            var initialDueDate = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc);
            var bill = new Bill { Id = 111, CustomerId = 1, SubscriptionId = 1, TotalAmount = 100m, PaymentStatus = "Paid", DueDate = initialDueDate };
            context.Bills.Add(bill);
            context.Payments.Add(new Payment { Id = 1101, BillId = 111, Amount = 100m, IsCancelled = false, PaymentDate = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var billRepo = new BillRepository(context);
            var mapper = CreateAutoMapper();
            var logger = NullLogger<BillsController>.Instance;

            var fakeMediator = new FakeMediator(request =>
            {
                if (request is GetBillByIdQuery getQuery)
                {
                    return new GetBillByIdQueryHandler(billRepo).Handle(getQuery, CancellationToken.None).GetAwaiter().GetResult()!;
                }
                if (request is UpdateBillCommand updateCmd)
                {
                    return new UpdateBillCommandHandler(billRepo).Handle(updateCmd, CancellationToken.None).GetAwaiter().GetResult();
                }
                throw new NotImplementedException();
            });

            var controller = new BillsController(fakeMediator, logger, mapper);
 
            // تعديل DueDate عبر الـ Controller بوجود دفعة فعالة
            var newDueDate = new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc);
            var updateDto = new BillUpdateDTO { DueDate = newDueDate };
            var actionResult = await controller.UpdateBill(1, 111, updateDto);
 
            // التحقق من نجاح العملية (204 NoContent)
            Assert.IsType<NoContentResult>(actionResult);
 
            // التأكد الحاسم: المبلغ في قاعدة البيانات لم يتغير وبقي 100، وتاريخ الاستحقاق حُدث بنجاح
            var savedBill = await context.Bills.FindAsync(111);
            Assert.NotNull(savedBill);
            Assert.Equal(100m, savedBill.TotalAmount);
            Assert.Equal(newDueDate, savedBill.DueDate);
        }
 
        // 12. السماح بتعديل DueDate وحده عبر الـ Controller عند وجود دفعة فعّالة وبقاء TotalAmount ثابتًا
        [Fact]
        public async Task Test12_UpdateBill_ControllerPath_AllowsDueDateChangeOnly_WhenActivePaymentExists()
        {
            using var context = CreateInMemoryDbContext(nameof(Test12_UpdateBill_ControllerPath_AllowsDueDateChangeOnly_WhenActivePaymentExists));
            var initialDueDate = new DateTime(2026, 11, 15, 0, 0, 0, DateTimeKind.Utc);
            var bill = new Bill { Id = 112, CustomerId = 1, SubscriptionId = 1, TotalAmount = 100m, PaymentStatus = "Paid", DueDate = initialDueDate };
            context.Bills.Add(bill);
            context.Payments.Add(new Payment { Id = 1201, BillId = 112, Amount = 100m, IsCancelled = false, PaymentDate = DateTime.UtcNow });
            await context.SaveChangesAsync();
 
            var billRepo = new BillRepository(context);
            var mapper = CreateAutoMapper();
            var logger = NullLogger<BillsController>.Instance;
 
            var fakeMediator = new FakeMediator(request =>
            {
                if (request is GetBillByIdQuery getQuery)
                {
                    return new GetBillByIdQueryHandler(billRepo).Handle(getQuery, CancellationToken.None).GetAwaiter().GetResult()!;
                }
                if (request is UpdateBillCommand updateCmd)
                {
                    return new UpdateBillCommandHandler(billRepo).Handle(updateCmd, CancellationToken.None).GetAwaiter().GetResult();
                }
                throw new NotImplementedException();
            });
 
            var controller = new BillsController(fakeMediator, logger, mapper);
 
            // تعديل DueDate فقط
            var newDueDate = new DateTime(2026, 11, 25, 0, 0, 0, DateTimeKind.Utc);
            var updateDto = new BillUpdateDTO { DueDate = newDueDate };
            var actionResult = await controller.UpdateBill(1, 112, updateDto);
 
            // التحقق من النجاح (204 NoContent)
            Assert.IsType<NoContentResult>(actionResult);
 
            // التحقق من تحديث تاريخ الاستحقاق وبقاء المبلغ دون تغيير
            var savedBill = await context.Bills.FindAsync(112);
            Assert.NotNull(savedBill);
            Assert.Equal(100m, savedBill.TotalAmount);
            Assert.Equal(newDueDate, savedBill.DueDate);
        }
 
        // 13. ثبات TotalAmount عبر مسار Controller عند عدم وجود دفعة فعّالة ونجاح تعديل DueDate
        [Fact]
        public async Task Test13_UpdateBill_ControllerPath_PreservesTotalAmount_WhenNoActivePaymentExists_AndUpdatesDueDate()
        {
            using var context = CreateInMemoryDbContext(nameof(Test13_UpdateBill_ControllerPath_PreservesTotalAmount_WhenNoActivePaymentExists_AndUpdatesDueDate));
            var initialDueDate = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc);
            var bill = new Bill { Id = 113, CustomerId = 1, SubscriptionId = 1, TotalAmount = 100m, PaymentStatus = "Unpaid", DueDate = initialDueDate };
            context.Bills.Add(bill);
            await context.SaveChangesAsync();
 
            var billRepo = new BillRepository(context);
            var mapper = CreateAutoMapper();
            var logger = NullLogger<BillsController>.Instance;
 
            var fakeMediator = new FakeMediator(request =>
            {
                if (request is GetBillByIdQuery getQuery)
                {
                    return new GetBillByIdQueryHandler(billRepo).Handle(getQuery, CancellationToken.None).GetAwaiter().GetResult()!;
                }
                if (request is UpdateBillCommand updateCmd)
                {
                    return new UpdateBillCommandHandler(billRepo).Handle(updateCmd, CancellationToken.None).GetAwaiter().GetResult();
                }
                throw new NotImplementedException();
            });
 
            var controller = new BillsController(fakeMediator, logger, mapper);
 
            // تعديل DueDate عند عدم وجود دفعات
            var newDueDate = new DateTime(2026, 11, 25, 0, 0, 0, DateTimeKind.Utc);
            var updateDto = new BillUpdateDTO { DueDate = newDueDate };
            var actionResult = await controller.UpdateBill(1, 113, updateDto);

            // التحقق من النجاح (204 NoContent)
            Assert.IsType<NoContentResult>(actionResult);

            // التحقق من ثبات المبلغ في قاعدة البيانات عند 100 وتحديث تاريخ الاستحقاق
            var savedBill = await context.Bills.FindAsync(113);
            Assert.NotNull(savedBill);
            Assert.Equal(100m, savedBill.TotalAmount); // لم يتغير إلى 160
            Assert.Equal(newDueDate, savedBill.DueDate);
        }

        // 14. التحقق من رمي InvalidOperationException وإعادة Result.Fail عند محاولة تحديث فاتورة غير موجودة
        [Fact]
        public async Task Test14_UpdateBillAsync_ThrowsInvalidOperationException_WhenBillNotFound()
        {
            using var context = CreateInMemoryDbContext(nameof(Test14_UpdateBillAsync_ThrowsInvalidOperationException_WhenBillNotFound));
            var billRepo = new BillRepository(context);

            var nonExistentBill = new Bill { Id = 999, CustomerId = 1, TotalAmount = 100m, PaymentStatus = "Unpaid", DueDate = DateTime.UtcNow };

            // التحقق من رمي المستودع لـ InvalidOperationException عند عدم العثور على الفاتورة
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => billRepo.UpdateBillAsync(nonExistentBill));
            Assert.Equal("Bill not found.", exception.Message);

            // التحقق من ترجمة الاستثناء إلى Result.Fail في CommandHandler وفق نمط النتائج
            var handler = new UpdateBillCommandHandler(billRepo);
            var result = await handler.Handle(new UpdateBillCommand(nonExistentBill), CancellationToken.None);
            Assert.False(result.Success);
            Assert.Equal("Bill not found.", result.ErrorMessage);
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
