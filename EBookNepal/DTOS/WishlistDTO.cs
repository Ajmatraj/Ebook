namespace EBookNepal.DTOS
{
    public class WishlistDTO
    {
        public string? WishlistId { get; set; }
        public string? BookId { get; set; }
        public string? BookTitle { get; set; }
        public string? BookAuthor { get; set; }
        public string? CoverImagePath { get; set; }

        public DateTime AddedDate { get; set; }
    }
}