using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EBookNepal.Entities
{
    public class Book
    {
        // ========================
        // PRIMARY KEY
        // ========================
        [Key]
        public string BookId { get; set; } = Guid.NewGuid().ToString();

        // ========================
        // BOOK INFO
        // ========================
        [Required]
        public required string Title { get; set; }

        [Required]
        public required string Author { get; set; }

        public string? Description { get; set; }

        [Required]
        public required string Genre { get; set; }

        [Required]
        public required string Language { get; set; }

        public string? Publisher { get; set; }
        public string? ISBN { get; set; }

        public DateTime? PublicationDate { get; set; }

        // ========================
        // INVENTORY & PRICING
        // ========================
        public int Stock { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OfferPrice { get; set; }

        public DateTime? OfferStartDate { get; set; }
        public DateTime? OfferEndDate { get; set; }

        public string? CoverImageUrl { get; set; }

        // ========================
        // SELLER RELATIONSHIP
        // ========================
        [Required]
        public required string SellerId { get; set; }

        [ForeignKey("SellerId")]
        public virtual User Seller { get; set; } = null!;

        // ========================
        // AUDIT FIELDS
        // ========================
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; } = false;

        public virtual ICollection<BookReview> Reviews { get; set; } = new List<BookReview>();

    }
}