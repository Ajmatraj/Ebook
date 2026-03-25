namespace EBookNepal.Entities
{
    public class Wishlist
    {
        public string WishlistId { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; }
        public User User { get; set; }
        public string BookId { get; set; }
        public Book Book { get; set; }
        public DateTime AddedDate { get; set; } = DateTime.UtcNow;
    }
}