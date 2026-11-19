namespace IspAppApi.DTOs.Customers
{
    public class CustomerCreateDTO
    {
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string BillingAddress { get; set; } = null!;
        public string ServiceAddress { get; set; } = null!;
    }
}
