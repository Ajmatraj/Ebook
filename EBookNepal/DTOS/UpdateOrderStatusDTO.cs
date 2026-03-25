using EBookNepal.Entities;

namespace EBookNepal.DTOS
{
    public class UpdateOrderStatusDTO
    {
        public string OrderId { get; set; }
        public string ClaimCode { get; set; }
        public OrderStatus OrderStatus { get; set; }
    }
}