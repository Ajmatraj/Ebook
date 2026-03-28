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

        public CartServices(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            UserManager<User> userManager)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        // =====================================================
        // ADD TO CART
        // =====================================================
        public void AddToCart(string userId, AddToCartDTO cartItem)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new Exception("User ID is required.");

            if (cartItem.Quantity <= 0)
                throw new Exception("Quantity must be at least 1.");

            var book = _context.Books
                .FirstOrDefault(b => b.BookId == cartItem.BookId && !b.IsDeleted);

            if (book == null)
                throw new KeyNotFoundException("Book not found.");

            var existingCartItem = _context.CartItems
                .FirstOrDefault(c => c.BookId == cartItem.BookId && c.UserId == userId);

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += cartItem.Quantity;
            }
            else
            {
                var newCartItem = new Cart
                {
                    CartItemId = Guid.NewGuid().ToString(),
                    BookId = cartItem.BookId,
                    UserId = userId,
                    Quantity = cartItem.Quantity,
                    AddedDate = DateTime.UtcNow,
                    SellerId = book.SellerId,
                    BookPrice = (book.OfferPrice.HasValue &&
                                 book.OfferStartDate <= DateTime.UtcNow &&
                                 book.OfferEndDate >= DateTime.UtcNow)
                                ? book.OfferPrice.Value
                                : book.Price
                };

                _context.CartItems.Add(newCartItem);
            }

            _context.SaveChanges();
        }

        // =====================================================
        // UPDATE CART ITEM
        // =====================================================
        public void UpdateCartItem(UpdateCartDTO dto)
        {
            var existingCartItem = _context.CartItems
                .FirstOrDefault(c => c.CartItemId == dto.CartItemId);

            if (existingCartItem == null)
                throw new KeyNotFoundException("Cart item not found.");

            if (dto.Quantity <= 0)
                throw new Exception("Quantity must be at least 1.");

            existingCartItem.Quantity = dto.Quantity;
            _context.SaveChanges();
        }

        // =====================================================
        // REMOVE FROM CART
        // =====================================================
        public void RemoveFromCart(string cartItemId)
        {
            var cartItem = _context.CartItems
                .FirstOrDefault(c => c.CartItemId == cartItemId);

            if (cartItem == null)
                throw new KeyNotFoundException("Cart item not found.");

            _context.CartItems.Remove(cartItem);
            _context.SaveChanges();
        }

        // =====================================================
        // CLEAR CART
        // =====================================================
        public void ClearCart(string userId)
        {
            var cartItems = _context.CartItems
                .Where(c => c.UserId == userId)
                .ToList();

            if (!cartItems.Any())
                throw new KeyNotFoundException("No items found in the cart.");

            _context.CartItems.RemoveRange(cartItems);
            _context.SaveChanges();
        }

        // =====================================================
        // GET TOTAL PRICE
        // =====================================================
        public decimal GetTotalPrice(string userId)
        {
            var cartItems = _context.CartItems
                .Include(c => c.Book)
                .Where(c => c.UserId == userId && !c.Book.IsDeleted)
                .ToList();

            decimal totalPrice = 0;

            foreach (var c in cartItems)
            {
                var price = (c.Book.OfferPrice.HasValue &&
                             c.Book.OfferStartDate <= DateTime.UtcNow &&
                             c.Book.OfferEndDate >= DateTime.UtcNow)
                    ? c.Book.OfferPrice.Value
                    : c.Book.Price;

                totalPrice += price * c.Quantity;
            }

            // Bulk discount: 5% if total quantity >= 5
            if (cartItems.Sum(c => c.Quantity) >= 5)
            {
                totalPrice *= 0.95m;
            }

            return totalPrice;
        }

        // =====================================================
        // GET CART ITEMS
        // =====================================================
        public IEnumerable<CartDTO> GetCartItemsByUserId(string userId)
        {
            return _context.CartItems
                .Include(c => c.Book)
                .AsNoTracking()
                .Where(c => c.UserId == userId && !c.Book.IsDeleted)
                .Select(c => new CartDTO
                {
                    CartItemId = c.CartItemId,
                    BookId = c.BookId,
                    BookTitle = c.Book.Title,
                    BookAuthor = c.Book.Author,
                    CoverImagePath = c.Book.CoverImageUrl,
                    Quantity = c.Quantity,

                    UnitPrice = c.BookPrice,
                    SellerId = c.SellerId,

                    TotalPrice = ((c.Book.OfferPrice.HasValue &&
                                   c.Book.OfferStartDate <= DateTime.UtcNow &&
                                   c.Book.OfferEndDate >= DateTime.UtcNow)
                                  ? c.Book.OfferPrice.Value
                                  : c.Book.Price) * c.Quantity,

                    AddedDate = c.AddedDate
                })
                .ToList();
        }

        // =====================================================
        // GET SINGLE CART ITEM
        // =====================================================
        public Cart GetCartItemById(string cartItemId)
        {
            var cartItem = _context.CartItems
                .Include(c => c.Book)
                .FirstOrDefault(c => c.CartItemId == cartItemId);

            if (cartItem == null)
                throw new KeyNotFoundException("Cart item not found.");

            return cartItem;
        }
    }
}