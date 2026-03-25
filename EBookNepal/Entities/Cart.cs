using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EBookNepal.Entities
{
    public class Cart
    {
        [Key]
        public string CartItemId { get; set; } = Guid.NewGuid().ToString();
        public string? BookId { get; set; }
        [ForeignKey("BookId")]
        public Book? Book { get; set; }
        public decimal? BookPrice { get; set; }
        public string? UserId { get; set; }
        public int Quantity { get; set; }
        public DateTime AddedDate { get; set; } = DateTime.UtcNow;
    }
}