namespace IspAppApi.DTOs.Payments
{
    public class PaymentCreateDTO
    {
        public DateTime? PaymentDate { get; set; } = DateTime.Now;
        public decimal Amount { get; set; }
        public int BillId { get; set; }
    }
}
