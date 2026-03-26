namespace EBookNepal.DTOS
{
    public class AddBookDTO
    {
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Description { get; set; }
        public string? Genre { get; set; }
        public string? Language { get; set; }
        public string? Publisher { get; set; }
        public string? ISBN { get; set; }

        // stays string because form input sends text
        public string? PublicationDate { get; set; }

        public decimal Price { get; set; }
        public int Stock { get; set; }

        public IFormFile? CoverImage { get; set; }
    }
}