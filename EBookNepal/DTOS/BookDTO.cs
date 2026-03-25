namespace EBookNepal.DTOS
{
    public class BookDTO
    {
        public string BookId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Stock { get; set; }
        public string Author { get; set; }
        public string Genre { get; set; }
        public string Language { get; set; }
        public string ISBN { get; set; }
        public string Publisher { get; set; }
        public string PublicationDate { get; set; }
        public decimal Price { get; set; }
        public decimal? OfferPrice { get; set; }
        public string? CoverImagePath { get; set; }
    }
}