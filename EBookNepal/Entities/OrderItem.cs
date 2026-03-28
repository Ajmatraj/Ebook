using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EBookNepal.Entities
{
    public class OrderItem
    {
        [Key]
        public string OrderItemId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string OrderId { get; set; }

        [Required]
        public string BookId { get; set; }

        [Required]
        public string SellerId { get; set; }

        public string BookTitle { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BookPrice { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        // ========================
        // NAVIGATION
        // ========================
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        [ForeignKey("BookId")]
        public virtual Book Book { get; set; }

        [ForeignKey("SellerId")]
        public virtual User? Seller { get; set; }
    }
}