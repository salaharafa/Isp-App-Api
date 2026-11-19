using IspAppApi.Application.Common;
using IspAppApi.Domain.Entities;

namespace IspAppApi.Application.Interfaces
{
    public interface IPaymentRepository
    {
        Task<List<Payment>> GetPaymentsAsync();
        Task<Payment?> GetPaymentAsync(int id);
        Task<Result<Payment>> AddPaymentAsync(Payment payment);
        Task<Result> CancelPaymentAsync(int paymentId, string cancellationReason);
    }
}
