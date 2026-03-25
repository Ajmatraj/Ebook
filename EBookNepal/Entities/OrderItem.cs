namespace EBookNepal.Entities
{
    public class OrderItem
    {
        public string OrderItemId { get; set; } = Guid.NewGuid().ToString();
        public string OrderId { get; set; }
        public string BookId { get; set; }
        public string BookTitle { get; set; }
        public decimal BookPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }

        // Navigation property
        public virtual Order Order { get; set; }
    }
}