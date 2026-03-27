namespace EBookNepal.DTOS
{
    public class CartDTO
    {
        public string CartItemId { get; set; }
        public string BookId { get; set; }

        public string BookTitle { get; set; }
        public string BookAuthor { get; set; }
        public string CoverImagePath { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }

        public DateTime AddedDate { get; set; }
    }
}