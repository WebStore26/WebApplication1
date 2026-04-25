namespace WebApplication1.Models
{
    public class Item
    {
        public long Id { get; set; }

        public string Name { get; set; } = "";

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public string Currency { get; set; } = "ILS";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
