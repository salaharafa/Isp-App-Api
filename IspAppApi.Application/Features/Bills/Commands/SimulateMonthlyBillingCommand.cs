using IspAppApi.Application.Common;
using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IspAppApi.Application.Features.Bills.Commands
{
    public record SimulateMonthlyBillingCommand(int Year, int Month, int? CustomerId = null) 
        : IRequest<Result<MonthlyBillingSimulationResult>>;

    public class MonthlyBillingSimulationResult
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int ProcessedSubscriptionsCount { get; set; }
        public int BillsGeneratedCount { get; set; }
        public int SubscriptionsSuspendedCount { get; set; }
        public int SkippedSubscriptionsCount { get; set; }
        public List<int> GeneratedBillIds { get; set; } = new();
        public List<int> SuspendedSubscriptionIds { get; set; } = new();
        public List<string> Details { get; set; } = new();
    }

    public class SimulateMonthlyBillingCommandHandler 
        : IRequestHandler<SimulateMonthlyBillingCommand, Result<MonthlyBillingSimulationResult>>
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IBillRepository _billRepository;

        public SimulateMonthlyBillingCommandHandler(
            ISubscriptionRepository subscriptionRepository,
            IBillRepository billRepository)
        {
            _subscriptionRepository = subscriptionRepository;
            _billRepository = billRepository;
        }

        public async Task<Result<MonthlyBillingSimulationResult>> Handle(
            SimulateMonthlyBillingCommand request, 
            CancellationToken cancellationToken)
        {
            if (request.Month < 1 || request.Month > 12)
            {
                return Result.Fail<MonthlyBillingSimulationResult>("Month must be between 1 and 12.");
            }

            if (request.Year < 2000 || request.Year > 2100)
            {
                return Result.Fail<MonthlyBillingSimulationResult>("Year is out of valid range.");
            }

            var periodStart = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = periodStart.AddMonths(1).AddTicks(-1);

            List<Subscription> subscriptions;
            if (request.CustomerId.HasValue)
            {
                subscriptions = await _subscriptionRepository.GetSubscriptionsForCustomerAsync(request.CustomerId.Value);
            }
            else
            {
                subscriptions = await _subscriptionRepository.GetSubscriptionsAsync();
            }

            var result = new MonthlyBillingSimulationResult
            {
                Year = request.Year,
                Month = request.Month,
                ProcessedSubscriptionsCount = subscriptions.Count
            };

            var newBills = new List<Bill>();

            foreach (var sub in subscriptions)
            {
                // 1. استبعاد الاشتراكات الملغاة نهائياً
                if (sub.Status == SubscriptionStatus.Cancelled)
                {
                    result.SkippedSubscriptionsCount++;
                    result.Details.Add($"Subscription {sub.Id} ({sub.Name}) is Cancelled. Skipped.");
                    continue;
                }

                // 2. استبعاد الاشتراكات المعلقة مسبقاً (لا تصدر فواتير جديدة للاشتراك أثناء تعليقه)
                if (sub.Status == SubscriptionStatus.Suspended)
                {
                    result.SkippedSubscriptionsCount++;
                    result.Details.Add($"Subscription {sub.Id} ({sub.Name}) is already Suspended. Skipped.");
                    continue;
                }

                // 3. التحقق من صلاحية تاريخ بداية ونهاية الاشتراك بالنسبة للفترة المحددة
                if (sub.StartDate > periodEnd)
                {
                    result.SkippedSubscriptionsCount++;
                    result.Details.Add($"Subscription {sub.Id} starts after this period ({sub.StartDate:yyyy-MM-dd}). Skipped.");
                    continue;
                }

                if (sub.EndDate.HasValue && sub.EndDate.Value < periodStart)
                {
                    result.SkippedSubscriptionsCount++;
                    result.Details.Add($"Subscription {sub.Id} ended before this period ({sub.EndDate.Value:yyyy-MM-dd}). Skipped.");
                    continue;
                }

                // 4. التحقق من الديون السابقة غير المدفوعة للاشتراك:
                // إذا كان للاشتراك أي فاتورة صادرة في فترة سابقة (قبل هذا الشهر) ولم تُسدَّد بالكامل -> يُعلَّق فوراً ولا تصدر له فاتورة
                bool hasUnpaidPastDebt = sub.Bills != null && sub.Bills.Any(b => 
                    b.CreatedDate < periodStart && b.PaymentStatus != "Paid");

                if (hasUnpaidPastDebt)
                {
                    sub.Status = SubscriptionStatus.Suspended;
                    await _subscriptionRepository.UpdateSubscriptionAsync(sub);

                    result.SubscriptionsSuspendedCount++;
                    result.SuspendedSubscriptionIds.Add(sub.Id);
                    result.Details.Add($"Subscription {sub.Id} ({sub.Name}) has unpaid past debt. Suspended and skipped new bill.");
                    continue;
                }

                // 5. منع التكرار: منع إصدار أكثر من فاتورة للاشتراك نفسه عن الفترة نفسها
                bool alreadyBilledForPeriod = sub.Bills != null && sub.Bills.Any(b => 
                    b.CreatedDate.Year == request.Year && b.CreatedDate.Month == request.Month);

                if (alreadyBilledForPeriod)
                {
                    result.SkippedSubscriptionsCount++;
                    result.Details.Add($"Subscription {sub.Id} ({sub.Name}) already has a bill for {request.Year}-{request.Month:D2}. Skipped.");
                    continue;
                }

                // 6. إصدار الفاتورة المستحقة بسعر الباقة المعتمد
                if (sub.ServicePlan == null)
                {
                    result.SkippedSubscriptionsCount++;
                    result.Details.Add($"Subscription {sub.Id} ({sub.Name}) has no ServicePlan. Skipped.");
                    continue;
                }

                var bill = new Bill
                {
                    CustomerId = sub.CustomerId,
                    SubscriptionId = sub.Id,
                    TotalAmount = sub.ServicePlan.Price,
                    PaymentStatus = "Unpaid",
                    CreatedDate = periodStart,
                    DueDate = periodStart.AddDays(15)
                };

                newBills.Add(bill);
                result.BillsGeneratedCount++;
                result.Details.Add($"Issued bill for Subscription {sub.Id} with amount {bill.TotalAmount}.");
            }

            if (newBills.Any())
            {
                await _billRepository.AddBillsRangeAsync(newBills);
                result.GeneratedBillIds = newBills.Select(b => b.Id).ToList();
            }

            return Result.Ok(result);
        }
    }
}
