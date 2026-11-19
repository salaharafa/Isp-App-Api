namespace IspAppApi.DTOs.Customers
{
    public class CustomerReadDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
    }
}
