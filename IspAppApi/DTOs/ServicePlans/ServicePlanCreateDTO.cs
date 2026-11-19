namespace IspAppApi.DTOs.ServicePlans
{
    public class ServicePlanCreateDTO
    {
        public string Name { get; set; } = null!;
        public int? Speed { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = null!;
    }
}
