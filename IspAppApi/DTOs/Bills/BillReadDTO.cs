namespace IspAppApi.DTOs.Bills
{
    public class BillReadDTO
    {
        public int Id { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime DueDate { get; set; }
        public string PaymentStatus { get; set; }
    }
}
