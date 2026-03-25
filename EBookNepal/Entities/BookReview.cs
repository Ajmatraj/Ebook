using System.ComponentModel.DataAnnotations;

namespace EBookNepal.Entities
{
    public class BookReview
    {
        [Key]
        public string ReviewId { get; set; } = Guid.NewGuid().ToString();
        public string? BookId { get; set; }
        public string? UserId { get; set; }
        public string? ReviewText { get; set; }
        public int? Rating { get; set; } // Rating out of 5
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Book Book { get; set; }
    }
}
