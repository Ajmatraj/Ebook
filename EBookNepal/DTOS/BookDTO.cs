namespace EBookNepal.DTOS
{
    public class BookDTO
    {
        public string? BookId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int Stock { get; set; }
        public string? Author { get; set; }
        public string? Genre { get; set; }
        public string? Language { get; set; }
        public string? ISBN { get; set; }
        public string? Publisher { get; set; }

        // Date should stay string for API responses
        public string? PublicationDate { get; set; }

        public decimal Price { get; set; }
        public decimal? OfferPrice { get; set; }
        public DateTime? OfferStartDate { get; set; } // ✅ add
        public DateTime? OfferEndDate { get; set; }   // ✅ add
        public string? CoverImagePath { get; set; }

        public string? SellerId { get; set; }
        public string? SellerName { get; set; }
    }
}