namespace WebApplication1.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string OrderId { get; set; } = Guid.NewGuid().ToString();

        public decimal Amount { get; set; }

        public bool IsPaid { get; set; }

        public long ItemId { get; set; }

        public string Status { get; set; } = "Pending"; // Pending / Completed / Failed

        public string PaymentProvider { get; set; } = "PayPal";

        public string? PaymentTransactionId { get; set; } // PayPal txn_id

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }

        public string? CustomerEmail { get; set; }

        public string? Description { get; set; }
    }
}
