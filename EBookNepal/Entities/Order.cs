using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EBookNepal.Entities
{
    public class Order
    {
        [Key]
        public string OrderId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public DateTime CheckedOutTime { get; set; } = DateTime.UtcNow;

        public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;

        public string? ClaimCode { get; set; }

        // ========================
        // NAVIGATION
        // ========================
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}