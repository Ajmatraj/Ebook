using System.Security.Cryptography.X509Certificates;

namespace EBookNepal.Entities
{
    public class Book
    {
        public string BookId { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string Genre { get; set; }
        public string Language { get; set; }
        public string Publisher { get; set; }
        public string ISBN { get; set; }
        public string PublicationDate { get; set; }
        public int Stock { get; set; }
        public decimal Price { get; set; }
        public decimal? OfferPrice { get; set; }
        public DateTime? OfferStartDate { get; set; }
        public DateTime? OfferEndDate { get; set; }
        public string? CoverImageUrl { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public string UpdatedDate { get; set; }
    }
}
