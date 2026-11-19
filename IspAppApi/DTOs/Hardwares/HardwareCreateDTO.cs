namespace IspAppApi.DTOs.Hardwares
{
    public class HardwareCreateDTO
    {
        public string Name { get; set; } = null!;
        public string SerialNumber { get; set; } = null!;
        public DateTime DateOfReceipt { get; set; } = DateTime.UtcNow;
        public DateTime? DateOfReturn { get; set; }
        public int CustomerId { get; set; }
    }
}
