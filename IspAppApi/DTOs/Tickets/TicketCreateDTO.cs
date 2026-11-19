namespace IspAppApi.DTOs.Tickets
{
    public class TicketCreateDTO
    {
        public string Name { get; set; } = null!;
        public int? CustomerId { get; set; }
    }
}
