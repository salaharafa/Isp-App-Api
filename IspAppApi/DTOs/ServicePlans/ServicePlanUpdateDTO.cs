namespace IspAppApi.DTOs.ServicePlans
{
    public class ServicePlanUpdateDTO
    {
        public string Name { get; set; } = null!;
        public int? Speed { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = null!;
        public bool IsActive { get; set; }
    }
}
