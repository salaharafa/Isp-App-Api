namespace IspAppApi.DTOs.Tickets
{
    public class TicketReadDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public TaskStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? CustomerId { get; set; }
    }
}
