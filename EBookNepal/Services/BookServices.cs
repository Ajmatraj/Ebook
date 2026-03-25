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

        public BookServices(ApplicationDbContext context, UserManager<User> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public IEnumerable<BookDTO> GetBooks()
        {
            try
            {
                return _context.Books
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
                        PublicationDate = b.PublicationDate,
                        Price = (b.OfferPrice.HasValue && b.OfferStartDate <= DateTime.UtcNow && b.OfferEndDate >= DateTime.UtcNow)
                            ? b.OfferPrice.Value
                            : b.Price,
                        Stock = b.Stock,
                        CoverImagePath = b.CoverImageUrl // Use Cloudinary URL
                    }).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving books: " + ex.Message);
            }
        }

        // Now expects the Cloudinary URL from the controller
        public void AddBook(AddBookDTO bookDto, string coverImageUrl)
        {
            try
            {
                var currentUserId = _userManager.GetUserId(_httpContextAccessor.HttpContext?.User);
                var safeUserId = string.IsNullOrEmpty(currentUserId) ? "System" : currentUserId;

                var book = new Book
                {
                    BookId = Guid.NewGuid().ToString(),
                    Title = bookDto.Title,
                    Author = bookDto.Author,
                    Description = bookDto.Description,
                    Genre = bookDto.Genre,
                    Language = bookDto.Language,
                    ISBN = bookDto.ISBN,
                    CreatedBy = safeUserId,
                    Publisher = bookDto.Publisher,
                    PublicationDate = bookDto.PublicationDate,
                    CoverImageUrl = coverImageUrl,
                    Price = bookDto.Price,
                    Stock = bookDto.Stock,
                    CreatedDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    UpdatedBy = safeUserId,
                    UpdatedDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                };
                _context.Books.Add(book);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception("Error adding book: " + ex.Message);
            }
        }

        // Now expects the Cloudinary URL from the controller (if updated)
        public void UpdateBook(UpdateBookDTO bookDto, string coverImageUrl = null)
        {
            try
            {
                var book = _context.Books.FirstOrDefault(b => b.BookId == bookDto.BookId);
                if (book == null)
                {
                    throw new Exception("Book not found.");
                }

                // Update book properties
                book.Title = bookDto.Title;
                book.Description = bookDto.Description;
                book.Stock = bookDto.Stock;
                book.Author = bookDto.Author;
                book.Genre = bookDto.Genre;
                book.Language = bookDto.Language;
                book.ISBN = bookDto.ISBN;
                book.Publisher = bookDto.Publisher;
                book.Price = bookDto.Price;
                book.PublicationDate = bookDto.PublicationDate;

                if (!string.IsNullOrEmpty(coverImageUrl))
                {
                    book.CoverImageUrl = coverImageUrl; // Update Cloudinary URL if provided
                }

                var currentUserId = _userManager.GetUserId(_httpContextAccessor.HttpContext.User) ?? "System";
                book.UpdatedBy = currentUserId;
                book.UpdatedDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                _context.Books.Update(book);

                try
                {
                    _context.SaveChanges();
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw new Exception("The book was modified or deleted by another process. Please reload and try again.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating book: " + ex.Message);
            }
        }

        public void AddToWishlist(string userId, string bookId)
        {
            var book = _context.Books.FirstOrDefault(b => b.BookId == bookId);
            if (book == null)
            {
                throw new Exception("Book not found.");
            }

            var wishlistItem = _context.Wishlists.FirstOrDefault(w => w.UserId == userId && w.BookId == bookId);
            if (wishlistItem != null)
            {
                throw new Exception("Book is already in the wishlist.");
            }

            var wishlist = new Wishlist
            {
                UserId = userId,
                BookId = bookId
            };

            _context.Wishlists.Add(wishlist);
            _context.SaveChanges();
        }

        public IEnumerable<WishlistDTO> GetWishlist(string userId)
        {
            var wishlist = _context.Wishlists
                .Where(w => w.UserId == userId)
                .Select(w => new WishlistDTO
                {
                    WishlistId = w.WishlistId,
                    BookId = w.BookId,
                    BookTitle = w.Book.Title,
                    BookAuthor = w.Book.Author,
                    AddedDate = w.AddedDate
                })
                .ToList();

            return wishlist;
        }

        public void RemoveFromWishlist(string wishlistId)
        {
            var wishlistItem = _context.Wishlists.FirstOrDefault(w => w.WishlistId == wishlistId);
            if (wishlistItem == null)
            {
                throw new Exception("Wishlist item not found.");
            }

            _context.Wishlists.Remove(wishlistItem);
            _context.SaveChanges();
        }

        public void SetOffer(SetOfferDTO offerDto)
        {
            var book = _context.Books.FirstOrDefault(b => b.BookId == offerDto.BookId);
            if (book == null)
            {
                throw new Exception("Book not found.");
            }

            if (offerDto.OfferStartDate >= offerDto.OfferEndDate)
            {
                throw new Exception("Offer start date must be earlier than the end date.");
            }

            book.OfferPrice = offerDto.OfferPrice;
            book.OfferStartDate = offerDto.OfferStartDate;
            book.OfferEndDate = offerDto.OfferEndDate;
            book.UpdatedBy = "Admin"; // Replace with the logged-in admin's ID if available
            book.UpdatedDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            _context.Books.Update(book);
            _context.SaveChanges();
        }

        public IEnumerable<BookDTO> GetPopularBooks()
        {
            try
            {
                // Query to rank books based on the number of users adding them to the wishlist
                var popularBooks = _context.Wishlists
                    .GroupBy(w => w.BookId)
                    .Select(g => new
                    {
                        BookId = g.Key,
                        WishlistCount = g.Count()
                    })
                    .OrderByDescending(b => b.WishlistCount)
                    .Take(10) // Get the top 10 books
                    .Join(_context.Books, 
                        wishlist => wishlist.BookId, 
                        book => book.BookId, 
                        (wishlist, book) => new BookDTO
                        {
                            BookId = book.BookId,
                            Title = book.Title,
                            Author = book.Author,
                            Description = book.Description,
                            Genre = book.Genre,
                            Language = book.Language,
                            ISBN = book.ISBN,
                            Publisher = book.Publisher,
                            PublicationDate = book.PublicationDate,
                            Price = (book.OfferPrice.HasValue && book.OfferStartDate <= DateTime.UtcNow && book.OfferEndDate >= DateTime.UtcNow)
                                ? book.OfferPrice.Value
                                : book.Price,
                            Stock = book.Stock,
                            CoverImagePath = book.CoverImageUrl // Use Cloudinary URL
                        })
                    .ToList();

                return popularBooks;
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving popular books: " + ex.Message);
            }
        }

        public IEnumerable<OnSaleBookDTO> GetOnSaleBooks()
        {
            try
            {
                // Query to get books with active offer prices
                var onSaleBooks = _context.Books
                    .Where(b => b.OfferPrice.HasValue && b.OfferStartDate <= DateTime.UtcNow && b.OfferEndDate >= DateTime.UtcNow)
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
                        PublicationDate = b.PublicationDate,
                        OfferPrice = b.OfferPrice.Value, // Use the offer price
                        ActualPrice = b.Price, // Include the original price for comparison
                        Stock = b.Stock,
                        CoverImagePath = b.CoverImageUrl // Use Cloudinary URL
                    })
                    .ToList();

                return onSaleBooks;
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving on-sale books: " + ex.Message);
            }
        }
    }
}