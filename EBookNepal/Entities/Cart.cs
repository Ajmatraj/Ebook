using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EBookNepal.Entities
{
    public class Cart
    {
        [Key]
        public string CartItemId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string BookId { get; set; }

        [Required]
        public string UserId { get; set; }

        public int Quantity { get; set; }

        [Required]
        public string SellerId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BookPrice { get; set; }

        public DateTime AddedDate { get; set; } = DateTime.UtcNow;

        // ========================
        // NAVIGATION
        // ========================
        [ForeignKey("BookId")]
        public virtual Book Book { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
       
        [ForeignKey("SellerId")]
        public virtual User? Seller { get; set; }
    }
}