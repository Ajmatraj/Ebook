namespace EBookNepal.DTOS
{
    public class OnSaleBookDTO
    {
        public string BookId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string Genre { get; set; }
        public string Language { get; set; }
        public string ISBN { get; set; }
        public string Publisher { get; set; }
        public string PublicationDate { get; set; }
        public decimal OfferPrice { get; set; } // Offer price
        public decimal ActualPrice { get; set; } // Original price
        public int Stock { get; set; }
        public string CoverImagePath { get; set; }
    }
}