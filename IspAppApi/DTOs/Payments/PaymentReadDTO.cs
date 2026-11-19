namespace IspAppApi.DTOs.Payments
{
    public class PaymentReadDTO
    {
        public int Id { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public int BillId { get; set; }
        public bool IsCancelled { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? CancelledAt { get; set; }
    }
}
