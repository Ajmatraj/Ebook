namespace EBookNepal.Entities
{
    public class Order
    {
        public string OrderId { get; set; }
        public string UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CheckedOutTime { get; set; }
        public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;
        public string ClaimCode { get; set; }

        // Navigation property
        public virtual ICollection<OrderItem> OrderItems { get; set; }
    }
}