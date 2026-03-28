namespace EBookNepal.DTOS
{
    public class AddToCartDTO
    {
        public string BookId { get; set; }
        public int Quantity { get; set; }
        public string? SellerId { get; set; }
    }
}