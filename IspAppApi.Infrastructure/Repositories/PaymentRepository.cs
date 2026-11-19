using IspAppApi.Application.Common;
using IspAppApi.Application.Interfaces;
using IspAppApi.Domain.Entities;
using IspAppApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IspAppApi.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly IspAppApiDbContext context;

        public PaymentRepository(IspAppApiDbContext context)
        {
            this.context = context;
        }

        public async Task<List<Payment>> GetPaymentsAsync()
        {
            return await context.Payments
                                .ToListAsync();
        }

        public async Task<Payment?> GetPaymentAsync(int id)
        {
            return await context.Payments
                                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Result<Payment>> AddPaymentAsync(Payment payment)
        {
            if (payment.Amount <= 0)
            {
                return Result.Fail<Payment>("Payment amount must be greater than zero.");
            }

            var bill = await context.Bills.FirstOrDefaultAsync(b => b.Id == payment.BillId);
            if (bill == null)
            {
                return Result.Fail<Payment>("Bill not found.");
            }

            bool hasActivePayment = await context.Payments.AnyAsync(p => p.BillId == payment.BillId && !p.IsCancelled);
            if (hasActivePayment)
            {
                return Result.Fail<Payment>("Bill already has an active payment.");
            }

            if (payment.Amount != bill.TotalAmount)
            {
                return Result.Fail<Payment>($"Payment amount ({payment.Amount}) must match the bill total amount ({bill.TotalAmount}). Partial payments are not supported.");
            }

            if (bill.PaymentStatus == "Paid")
            {
                return Result.Fail<Payment>("Bill is already marked as Paid.");
            }

            payment.IsCancelled = false;
            payment.CancellationReason = null;
            payment.CancelledAt = null;

            bill.PaymentStatus = "Paid";
            context.Payments.Add(payment);

            await context.SaveChangesAsync();
            return Result.Ok(payment);
        }

        public async Task<Result> CancelPaymentAsync(int paymentId, string cancellationReason)
        {
            if (string.IsNullOrWhiteSpace(cancellationReason))
            {
                return Result.Fail("Cancellation reason is required.");
            }

            if (cancellationReason.Length > 500)
            {
                return Result.Fail("Cancellation reason cannot exceed 500 characters.");
            }

            var payment = await context.Payments.FirstOrDefaultAsync(p => p.Id == paymentId);
            if (payment == null)
            {
                return Result.Fail("Payment not found.");
            }

            if (payment.IsCancelled)
            {
                return Result.Fail("Payment is already cancelled.");
            }

            payment.IsCancelled = true;
            payment.CancellationReason = cancellationReason.Trim();
            payment.CancelledAt = DateTime.UtcNow;

            var bill = await context.Bills.FirstOrDefaultAsync(b => b.Id == payment.BillId);
            if (bill != null)
            {
                bool hasOtherActivePayments = await context.Payments
                    .AnyAsync(p => p.BillId == payment.BillId && p.Id != paymentId && !p.IsCancelled);

                if (!hasOtherActivePayments)
                {
                    bill.PaymentStatus = "Unpaid";
                }
            }

            await context.SaveChangesAsync();
            return Result.Ok();
        }
    }
}
