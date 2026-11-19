namespace IspAppApi.DTOs.Hardwares
{
    public class HardwareUpdateDTO
    {
        public string Name { get; set; } = null!;
        public string SerialNumber { get; set; } = null!;
        public DateTime? DateOfReturn { get; set; }
    }
}
