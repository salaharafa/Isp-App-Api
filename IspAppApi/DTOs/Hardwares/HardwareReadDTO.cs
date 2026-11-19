namespace IspAppApi.DTOs.Hardwares
{
    public class HardwareReadDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string SerialNumber { get; set; } = null!;
        public DateTime DateOfReceipt { get; set; }
        public DateTime? DateOfReturn { get; set; }
        public int CustomerId { get; set; }
    }
}
