namespace EBookNepal.DTOS
{
    public class AddReviewDTO
    {
        public string BookId { get; set; }
        public string UserId { get; set; }
        public string ReviewText { get; set; }
        public int Rating { get; set; } // Rating out of 5
    }
}