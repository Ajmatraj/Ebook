using EBookNepal.Data;
using EBookNepal.DTOS;
using EBookNepal.Entities;
using EBookNepal.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EBookNepal.Services
{
    public class BookServices : IBookServices
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BookServices(
            ApplicationDbContext context,
            UserManager<User> userManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        // =====================================================
        // CURRENT USER
        // =====================================================
        private string GetCurrentUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return _userManager.GetUserId(user) ?? throw new Exception("User not authenticated.");
        }

        // =====================================================
        // DATE HELPER
        // =====================================================
        private static DateTime? ParseToUtc(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return null;

            var parsed = DateTime.Parse(dateString);

            return parsed.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                : parsed.ToUniversalTime();
        }

        // =====================================================
        // BOOK MAPPING
        // =====================================================
        private static BookDTO MapToBookDTO(Book b)
        {
            var isOfferActive =
                b.OfferPrice.HasValue &&
                b.OfferStartDate <= DateTime.UtcNow &&
                b.OfferEndDate >= DateTime.UtcNow;

            return new BookDTO
            {
                BookId = b.BookId,
                Title = b.Title,
                Author = b.Author,
                Description = b.Description,
                Genre = b.Genre,
                Language = b.Language,
                ISBN = b.ISBN,
                Publisher = b.Publisher,
                PublicationDate = b.PublicationDate?.ToString("yyyy-MM-dd"),
                Price = isOfferActive ? b.OfferPrice!.Value : b.Price,
                Stock = b.Stock,
                CoverImagePath = b.CoverImageUrl,
                SellerId = b.SellerId,
                SellerName = b.Seller?.Name
            };
        }

        // =====================================================
        // GET ALL BOOKS
        // =====================================================
        public IEnumerable<BookDTO> GetBooks()
        {
            return _context.Books
                .Include(b => b.Seller)
                .Where(b => !b.IsDeleted)
                .Select(b => MapToBookDTO(b))
                .ToList();
        }

        // =====================================================
        // GET BOOKS BY SELLER
        // =====================================================
        public IEnumerable<BookDTO> GetBooksBySeller(string sellerId)
        {
            if (string.IsNullOrWhiteSpace(sellerId))
                throw new Exception("Seller ID is required.");

            return _context.Books
                .Include(b => b.Seller)
                .Where(b => !b.IsDeleted && b.SellerId == sellerId)
                .Select(b => MapToBookDTO(b))
                .ToList();
        }

        // =====================================================
        // GET BOOK BY ID
        // =====================================================
        public BookDTO GetBookById(string bookId)
        {
            var book = _context.Books
                .Include(b => b.Seller)
                .FirstOrDefault(b => b.BookId == bookId && !b.IsDeleted);

            if (book == null)
                throw new Exception("Book not found.");

            return MapToBookDTO(book);
        }

        // =====================================================
        // ADD BOOK
        // =====================================================
        public void AddBook(AddBookDTO bookDto, string coverImageUrl)
        {
            var userId = GetCurrentUserId();

            var book = new Book
            {
                BookId = Guid.NewGuid().ToString(),
                Title = bookDto.Title,
                Author = bookDto.Author,
                Description = bookDto.Description,
                Genre = bookDto.Genre,
                Language = bookDto.Language,
                ISBN = bookDto.ISBN,
                Publisher = bookDto.Publisher,
                PublicationDate = ParseToUtc(bookDto.PublicationDate),
                CoverImageUrl = coverImageUrl,
                Price = bookDto.Price,
                Stock = bookDto.Stock,
                SellerId = userId,
                CreatedBy = userId,
                UpdatedBy = userId,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            _context.Books.Add(book);
            _context.SaveChanges();
        }

        // =====================================================
        // UPDATE BOOK
        // =====================================================
        public void UpdateBook(UpdateBookDTO bookDto, string coverImageUrl)
        {
            var currentUserId = GetCurrentUserId();

            var book = _context.Books.FirstOrDefault(b => b.BookId == bookDto.BookId);

            if (book == null)
                throw new Exception("Book not found.");

            if (book.SellerId != currentUserId)
                throw new Exception("You are not authorized to update this book.");

            book.Title = bookDto.Title;
            book.Description = bookDto.Description;
            book.Stock = bookDto.Stock;
            book.Author = bookDto.Author;
            book.Genre = bookDto.Genre;
            book.Language = bookDto.Language;
            book.ISBN = bookDto.ISBN;
            book.Publisher = bookDto.Publisher;
            book.Price = bookDto.Price;
            book.PublicationDate = ParseToUtc(bookDto.PublicationDate);

            if (!string.IsNullOrWhiteSpace(coverImageUrl))
                book.CoverImageUrl = coverImageUrl;

            book.UpdatedBy = currentUserId;
            book.UpdatedDate = DateTime.UtcNow;

            _context.SaveChanges();
        }

        // =====================================================
        // DELETE BOOK (SOFT)
        // =====================================================
        public void DeleteBook(string bookId)
        {
            var currentUserId = GetCurrentUserId();

            var book = _context.Books.FirstOrDefault(b => b.BookId == bookId && !b.IsDeleted);

            if (book == null)
                throw new Exception("Book not found.");

            if (book.SellerId != currentUserId)
                throw new Exception("You are not authorized to delete this book.");

            book.IsDeleted = true;
            book.UpdatedBy = currentUserId;
            book.UpdatedDate = DateTime.UtcNow;

            _context.SaveChanges();
        }

        // =====================================================
        // WISHLIST
        // =====================================================
        public void AddToWishlist(string bookId)
        {
            var userId = GetCurrentUserId();

            var exists = _context.Wishlists
                .Any(w => w.UserId == userId && w.BookId == bookId);

            if (exists)
                throw new Exception("Book already in wishlist.");

            _context.Wishlists.Add(new Wishlist
            {
                WishlistId = Guid.NewGuid().ToString(),
                UserId = userId,
                BookId = bookId,
                AddedDate = DateTime.UtcNow
            });

            _context.SaveChanges();
        }

        public IEnumerable<WishlistDTO> GetWishlist()
        {
            var userId = GetCurrentUserId();

            return _context.Wishlists
                .Include(w => w.Book)
                .Where(w => w.UserId == userId && !w.Book.IsDeleted)
                .Select(w => new WishlistDTO
                {
                    WishlistId = w.WishlistId,
                    BookId = w.BookId,
                    BookTitle = w.Book.Title,
                    BookAuthor = w.Book.Author,
                    AddedDate = w.AddedDate
                })
                .ToList();
        }

        public void RemoveFromWishlist(string wishlistId)
        {
            var userId = GetCurrentUserId();

            var item = _context.Wishlists
                .FirstOrDefault(w => w.WishlistId == wishlistId && w.UserId == userId);

            if (item == null)
                throw new Exception("Wishlist item not found.");

            _context.Wishlists.Remove(item);
            _context.SaveChanges();
        }

        // =====================================================
        // CART
        // =====================================================
        public void AddToCart(string bookId, int quantity)
        {
            var userId = GetCurrentUserId();

            if (quantity <= 0)
                throw new Exception("Quantity must be at least 1.");

            var book = _context.Books
                .FirstOrDefault(b => b.BookId == bookId && !b.IsDeleted);

            if (book == null)
                throw new Exception("Book not found.");

            if (book.Stock < quantity)
                throw new Exception($"Only {book.Stock} copies available.");

            var existing = _context.CartItems
                .FirstOrDefault(c => c.UserId == userId && c.BookId == bookId);

            if (existing != null)
            {
                var newQty = existing.Quantity + quantity;

                if (newQty > book.Stock)
                    throw new Exception("Not enough stock.");

                existing.Quantity = newQty;
            }
            else
            {
                _context.CartItems.Add(new Cart
                {
                    CartItemId = Guid.NewGuid().ToString(),
                    UserId = userId,
                    BookId = bookId,
                    Quantity = quantity,
                    AddedDate = DateTime.UtcNow
                });
            }

            _context.SaveChanges();
        }

        public IEnumerable<CartDTO> GetCart()
        {
            var userId = GetCurrentUserId();

            return _context.CartItems
                .Include(c => c.Book)
                .Where(c => c.UserId == userId && !c.Book.IsDeleted)
                .Select(c => new CartDTO
                {
                    CartItemId = c.CartItemId,
                    BookId = c.BookId,
                    BookTitle = c.Book.Title,
                    BookAuthor = c.Book.Author,
                    CoverImagePath = c.Book.CoverImageUrl,
                    Quantity = c.Quantity,

                    UnitPrice = c.Book.OfferPrice.HasValue &&
                                c.Book.OfferStartDate <= DateTime.UtcNow &&
                                c.Book.OfferEndDate >= DateTime.UtcNow
                                ? c.Book.OfferPrice.Value
                                : c.Book.Price,

                    TotalPrice = (
                        c.Book.OfferPrice.HasValue &&
                        c.Book.OfferStartDate <= DateTime.UtcNow &&
                        c.Book.OfferEndDate >= DateTime.UtcNow
                            ? c.Book.OfferPrice.Value
                            : c.Book.Price
                    ) * c.Quantity,

                    AddedDate = c.AddedDate
                })
                .ToList();
        }

        public void RemoveFromCart(string cartItemId)
        {
            var userId = GetCurrentUserId();

            var item = _context.CartItems
                .FirstOrDefault(c => c.CartItemId == cartItemId && c.UserId == userId);

            if (item == null)
                throw new Exception("Cart item not found.");

            _context.CartItems.Remove(item);
            _context.SaveChanges();
        }

        // =====================================================
        // OFFERS
        // =====================================================
        public void SetOffer(SetOfferDTO dto)
        {
            var book = _context.Books.FirstOrDefault(b => b.BookId == dto.BookId);

            if (book == null)
                throw new Exception("Book not found.");

            if (dto.OfferStartDate >= dto.OfferEndDate)
                throw new Exception("Invalid offer period.");

            book.OfferPrice = dto.OfferPrice;
            book.OfferStartDate = dto.OfferStartDate.ToUniversalTime();
            book.OfferEndDate = dto.OfferEndDate.ToUniversalTime();

            book.UpdatedBy = GetCurrentUserId();
            book.UpdatedDate = DateTime.UtcNow;

            _context.SaveChanges();
        }

        // =====================================================
        // POPULAR BOOKS
        // =====================================================
        public IEnumerable<BookDTO> GetPopularBooks()
        {
            var ids = _context.Wishlists
                .GroupBy(w => w.BookId)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => g.Key)
                .ToList();

            return _context.Books
                .Include(b => b.Seller)
                .Where(b => ids.Contains(b.BookId) && !b.IsDeleted)
                .Select(b => MapToBookDTO(b))
                .ToList();
        }

        // =====================================================
        // ON SALE BOOKS
        // =====================================================
        public IEnumerable<OnSaleBookDTO> GetOnSaleBooks()
        {
            return _context.Books
                .Where(b => !b.IsDeleted &&
                            b.OfferPrice.HasValue &&
                            b.OfferStartDate <= DateTime.UtcNow &&
                            b.OfferEndDate >= DateTime.UtcNow)
                .Select(b => new OnSaleBookDTO
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Author = b.Author,
                    Description = b.Description,
                    Genre = b.Genre,
                    Language = b.Language,
                    ISBN = b.ISBN,
                    Publisher = b.Publisher,

                    PublicationDate = b.PublicationDate.HasValue
                        ? b.PublicationDate.Value.ToString("yyyy-MM-dd")
                        : null,

                    OfferPrice = b.OfferPrice.Value,
                    ActualPrice = b.Price,
                    Stock = b.Stock,
                    CoverImagePath = b.CoverImageUrl
                })
                .ToList();
        }
    }
}