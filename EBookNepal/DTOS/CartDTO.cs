namespace EBookNepal.DTOS
{
    public class CartDTO
    {
        public string CartItemId { get; set; }
        public string BookId { get; set; }
        public string BookTitle { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
    }
}