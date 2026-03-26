using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EBookNepal.Entities
{
    public class Wishlist
    {
        [Key]
        public string WishlistId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; }

        [Required]
        public string BookId { get; set; }

        public DateTime AddedDate { get; set; } = DateTime.UtcNow;

        // ========================
        // NAVIGATION
        // ========================
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("BookId")]
        public virtual Book Book { get; set; }
    }
}