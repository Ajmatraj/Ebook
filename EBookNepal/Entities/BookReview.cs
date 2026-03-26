using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EBookNepal.Entities
{
    public class BookReview
    {
        [Key]
        public string ReviewId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string BookId { get; set; }

        [Required]
        public string UserId { get; set; }

        public string? ReviewText { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ========================
        // NAVIGATION PROPERTIES
        // ========================
        [ForeignKey("BookId")]
        public virtual Book Book { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}