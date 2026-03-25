namespace EBookNepal.Services
{
    using EBookNepal.Data;
    using EBookNepal.DTOS;
    using EBookNepal.Entities;
    using EBookNepal.Services.Interfaces;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;

    public class CartServices : ICartServices
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<User> _userManager;

        public CartServices(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, UserManager<User> userManager)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public void AddToCart(string userId, AddToCartDTO cartItem)
        {
            var book = _context.Books.FirstOrDefault(b => b.BookId == cartItem.BookId);

            if (book == null)
            {
                throw new KeyNotFoundException("Book not found.");
            }

            var existingCartItem = _context.CartItems
                .FirstOrDefault(c => c.BookId == cartItem.BookId && c.UserId == userId);

            var price = (book.OfferPrice.HasValue && book.OfferStartDate <= DateTime.UtcNow && book.OfferEndDate >= DateTime.UtcNow)
                ? book.OfferPrice.Value
                : book.Price;

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += cartItem.Quantity;
            }
            else
            {
                var newCartItem = new Cart
                {
                    BookId = cartItem.BookId,
                    UserId = userId,
                    Quantity = cartItem.Quantity,
                    BookPrice = price,
                    Book = book
                };
                _context.CartItems.Add(newCartItem);
            }

            _context.SaveChanges();
        }

        public void UpdateCartItem(Cart cartItem)
        {
            var existingCartItem = _context.CartItems.FirstOrDefault(c => c.CartItemId == cartItem.CartItemId);

            if (existingCartItem == null)
            {
                throw new KeyNotFoundException("Cart item not found.");
            }

            existingCartItem.Quantity = cartItem.Quantity;
            _context.SaveChanges();
        }

        public void RemoveFromCart(string cartItemId)
        {
            var cartItem = _context.CartItems.FirstOrDefault(c => c.CartItemId == cartItemId);

            if (cartItem == null)
            {
                throw new KeyNotFoundException("Cart item not found.");
            }

            _context.CartItems.Remove(cartItem);
            _context.SaveChanges();
        }

        public void ClearCart(string userId)
        {
            var cartItems = _context.CartItems.Where(c => c.UserId == userId).ToList();

            if (!cartItems.Any())
            {
                throw new KeyNotFoundException("No items found in the cart.");
            }

            _context.CartItems.RemoveRange(cartItems);
            _context.SaveChanges();
        }

        public decimal GetTotalPrice(string userId)
        {
            var cartItems = _context.CartItems
                .Include(c => c.Book)
                .Where(c => c.UserId == userId)
                .ToList();

            var totalQuantity = cartItems.Sum(c => c.Quantity);

            var totalPrice = cartItems.Sum(c =>
            {
                var price = (c.Book.OfferPrice.HasValue && c.Book.OfferStartDate <= DateTime.UtcNow && c.Book.OfferEndDate >= DateTime.UtcNow)
                    ? c.Book.OfferPrice.Value
                    : c.Book.Price;

                return price * c.Quantity;
            });

            if (totalQuantity >= 5)
            {
                totalPrice *= 0.95m;
            }

            return totalPrice;
        }

        public IEnumerable<CartDTO> GetCartItemsByUserId(string userId)
        {
            return _context.CartItems
                .Include(c => c.Book)
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .Select(c => new CartDTO
                {
                    CartItemId = c.CartItemId,
                    BookId = c.BookId,
                    BookTitle = c.Book.Title,
                    Quantity = c.Quantity,
                    Price = (c.Book.OfferPrice.HasValue && c.Book.OfferStartDate <= DateTime.UtcNow && c.Book.OfferEndDate >= DateTime.UtcNow)
                        ? c.Book.OfferPrice.Value
                        : c.Book.Price,
                    TotalPrice = ((c.Book.OfferPrice.HasValue && c.Book.OfferStartDate <= DateTime.UtcNow && c.Book.OfferEndDate >= DateTime.UtcNow)
                        ? c.Book.OfferPrice.Value
                        : c.Book.Price) * c.Quantity
                })
                .ToList();
        }

        public Cart GetCartItemById(string cartItemId)
        {
            var cartItem = _context.CartItems
                .Include(c => c.Book)
                .FirstOrDefault(c => c.CartItemId == cartItemId);

            if (cartItem == null)
            {
                throw new KeyNotFoundException("Cart item not found.");
            }

            return cartItem;
        }
    }
}
