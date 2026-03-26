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

        private string GetCurrentUserId()
        {
            return _userManager.GetUserId(_httpContextAccessor.HttpContext?.User) ?? "System";
        }

        // =============================
        // GET ALL BOOKS
        // =============================
        public IEnumerable<BookDTO> GetBooks()
        {
            return _context.Books
                .Include(b => b.Seller)
                .Where(b => !b.IsDeleted)
                .Select(b => new BookDTO
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

                    Price = (b.OfferPrice.HasValue &&
                             b.OfferStartDate <= DateTime.UtcNow &&
                             b.OfferEndDate >= DateTime.UtcNow)
                            ? b.OfferPrice.Value
                            : b.Price,

                    Stock = b.Stock,
                    CoverImagePath = b.CoverImageUrl,

                    SellerId = b.SellerId,
                    SellerName = b.Seller.Name
                })
                .ToList();
        }

        // =============================
        // ADD BOOK
        // =============================
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

                PublicationDate = string.IsNullOrEmpty(bookDto.PublicationDate)
                    ? null
                    : DateTime.Parse(bookDto.PublicationDate),

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

        // =============================
        // UPDATE BOOK
        // =============================
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

            book.PublicationDate = string.IsNullOrEmpty(bookDto.PublicationDate)
                ? null
                : DateTime.Parse(bookDto.PublicationDate);

            if (!string.IsNullOrEmpty(coverImageUrl))
                book.CoverImageUrl = coverImageUrl;

            book.UpdatedBy = currentUserId;
            book.UpdatedDate = DateTime.UtcNow;

            _context.SaveChanges();
        }

        // =============================
        // WISHLIST
        // =============================
        public void AddToWishlist(string bookId)
        {
            var userId = GetCurrentUserId();

            var exists = _context.Wishlists
                .Any(w => w.UserId == userId && w.BookId == bookId);

            if (exists)
                throw new Exception("Book already in wishlist.");

            var wishlist = new Wishlist
            {
                WishlistId = Guid.NewGuid().ToString(),
                UserId = userId,
                BookId = bookId,
                AddedDate = DateTime.UtcNow
            };

            _context.Wishlists.Add(wishlist);
            _context.SaveChanges();
        }

        public IEnumerable<WishlistDTO> GetWishlist()
        {
            var userId = GetCurrentUserId();

            return _context.Wishlists
                .Where(w => w.UserId == userId)
                .Include(w => w.Book)
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
            var item = _context.Wishlists.FirstOrDefault(w => w.WishlistId == wishlistId);
            if (item == null)
                throw new Exception("Wishlist item not found.");

            _context.Wishlists.Remove(item);
            _context.SaveChanges();
        }

        // =============================
        // OFFERS
        // =============================
        public void SetOffer(SetOfferDTO offerDto)
        {
            var book = _context.Books.FirstOrDefault(b => b.BookId == offerDto.BookId);

            if (book == null)
                throw new Exception("Book not found.");

            if (offerDto.OfferStartDate >= offerDto.OfferEndDate)
                throw new Exception("Invalid offer dates.");

            book.OfferPrice = offerDto.OfferPrice;
            book.OfferStartDate = offerDto.OfferStartDate;
            book.OfferEndDate = offerDto.OfferEndDate;

            book.UpdatedBy = GetCurrentUserId();
            book.UpdatedDate = DateTime.UtcNow;

            _context.SaveChanges();
        }

        // =============================
        // POPULAR BOOKS
        // =============================
        public IEnumerable<BookDTO> GetPopularBooks()
        {
            return _context.Wishlists
                .GroupBy(w => w.BookId)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => g.First().Book)
                .Include(b => b.Seller)
                .Select(b => new BookDTO
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
                    Price = b.OfferPrice ?? b.Price,
                    Stock = b.Stock,
                    CoverImagePath = b.CoverImageUrl,
                    SellerId = b.SellerId,
                    SellerName = b.Seller.Name
                })
                .ToList();
        }

        // =============================
        // ON SALE BOOKS
        // =============================
        public IEnumerable<OnSaleBookDTO> GetOnSaleBooks()
        {
            return _context.Books
                .Where(b => b.OfferPrice.HasValue &&
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