namespace IspAppApi.DTOs.ServicePlans
{
    public class ServicePlanReadDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int? Speed { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } = null!;
        public bool IsActive { get; set; }
    }
}
