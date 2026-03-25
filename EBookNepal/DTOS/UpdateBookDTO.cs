namespace EBookNepal.DTOS
{
    public class UpdateBookDTO
    {
        public string BookId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string Genre { get; set; }
        public string Language { get; set; }
        public string Publisher { get; set; }
        public string ISBN { get; set; }
        public string PublicationDate { get; set; }
        public int Stock { get; set; }
        public int Price { get; set; }
        public IFormFile? CoverImage { get; set; }
    }
}