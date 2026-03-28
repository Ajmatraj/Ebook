using EBookNepal.DTOS;
using EBookNepal.Entities;

namespace EBookNepal.Services.Interfaces
{
    public interface ICartServices
    {
        void AddToCart(string userId, AddToCartDTO cartItem);
        void UpdateCartItem(UpdateCartDTO cartItem);
        void RemoveFromCart(string cartItemId);
        void ClearCart(string userId);
        decimal GetTotalPrice(string userId);
        IEnumerable<CartDTO> GetCartItemsByUserId(string userId);
        Cart GetCartItemById(string cartItemId);
    }
}