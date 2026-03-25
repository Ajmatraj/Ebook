namespace EBookNepal.DTOS
{
    public class FilterBookDTO
    {
        public string? Title { get; set; }
        public string? Genre { get; set; }
        public string? Author { get; set; }
        public string? Description { get; set; }
        public string? Language { get; set; }
        public string? Publisher { get; set; }
        public string? ISBN { get; set; }
        public DateTime? PublicationDate { get; set; }
        public int? Stock { get; set; }
        public decimal? Price { get; set; }
        public decimal? OfferPrice { get; set; }
    }
}