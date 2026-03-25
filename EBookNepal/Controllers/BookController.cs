using CloudinaryDotNet;
using EBookNepal.Data;
using EBookNepal.DTOS;
using EBookNepal.Entities;
using EBookNepal.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EBookNepal.Controllers
{
    public class BookController : ControllerBase
    {
        private readonly IBookServices _bookServices;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<BookController> _logger;
        private readonly ApplicationDbContext _context;

        public BookController(IBookServices bookServices, IWebHostEnvironment webHostEnvironment, ILogger<BookController> logger, ApplicationDbContext context)
        {
            _bookServices = bookServices;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _context = context;
        }

        [HttpGet("GetBooks")]
        public IActionResult GetBooks()
        {
            try
            {
                var books = _bookServices.GetBooks();
                return Ok(books);
            }
            catch (Exception ex)
            {
                return BadRequest("Error retrieving books: " + ex.Message);
            }
        }

        [HttpGet("GetBookById")]
        public IActionResult GetBookById([FromQuery] string bookId)
        {
            try
            {
                if (string.IsNullOrEmpty(bookId))
                {
                    return BadRequest("Book ID is required.");
                }

                var book = _context.Books
                    .Where(b => b.BookId == bookId)
                    .Select(book => new BookDTO
                    {
                        BookId = book.BookId,
                        Title = book.Title,
                        Stock = book.Stock,
                        Description = book.Description,
                        Author = book.Author,
                        Genre = book.Genre,
                        Language = book.Language,
                        ISBN = book.ISBN,
                        Publisher = book.Publisher,
                        PublicationDate = book.PublicationDate,
                        Price = (book.OfferPrice.HasValue && book.OfferStartDate <= DateTime.UtcNow && book.OfferEndDate >= DateTime.UtcNow)
                            ? book.OfferPrice.Value
                            : book.Price,
                        CoverImagePath = book.CoverImageUrl
                    })
                    .FirstOrDefault();

                if (book == null)
                {
                    return NotFound("Book not found.");
                }

                return Ok(book);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("FilterBooks")]
        public IActionResult FilterBooks([FromQuery] FilterBookDTO filter)
        {
            try
            {
                var books = _bookServices.GetBooks();

                // Apply filters
                if (!string.IsNullOrEmpty(filter.Title))
                {
                    books = books.Where(b => b.Title.Contains(filter.Title, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(filter.Genre))
                {
                    books = books.Where(b => b.Genre.Contains(filter.Genre, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(filter.Author))
                {
                    books = books.Where(b => b.Author.Contains(filter.Author, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(filter.Description))
                {
                    books = books.Where(b => b.Description != null && b.Description.Contains(filter.Description, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(filter.Language))
                {
                    books = books.Where(b => b.Language.Contains(filter.Language, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(filter.Publisher))
                {
                    books = books.Where(b => b.Publisher.Contains(filter.Publisher, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(filter.ISBN))
                {
                    books = books.Where(b => b.ISBN.Contains(filter.ISBN, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (filter.PublicationDate.HasValue)
                {
                    books = books.Where(b =>
                        DateTime.TryParse(b.PublicationDate, out var bookDate) &&
                        bookDate.Date == filter.PublicationDate.Value.Date).ToList();
                }

                if (filter.Stock.HasValue)
                {
                    books = books.Where(b => b.Stock == filter.Stock.Value).ToList();
                }

                if (filter.Price.HasValue)
                {
                    books = books.Where(b => b.Price == filter.Price.Value).ToList();
                }

                if (filter.OfferPrice.HasValue)
                {
                    books = books.Where(b => b.OfferPrice.HasValue && b.OfferPrice.Value == filter.OfferPrice.Value).ToList();
                }

                return Ok(books);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("AddBook")]
        public async Task<IActionResult> AddBook([FromForm] AddBookDTO bookDto)
        {
            try
            {
                if (bookDto == null)
                {
                    return BadRequest("Book data is null.");
                }

                string imageUrl = null;

                // Upload to Cloudinary if image is provided
                if (bookDto.CoverImage != null && bookDto.CoverImage.Length > 0)
                {
                    var imageService = HttpContext.RequestServices.GetService(typeof(EBookNepal.Services.ImageServices)) as EBookNepal.Services.ImageServices;
                    var uploadResult = await imageService.UploadPhotoAsync(bookDto.CoverImage);
                    imageUrl = uploadResult.SecureUrl?.ToString();
                }

                // Use the service to add the book (service handles CreatedBy, etc.)
                _bookServices.AddBook(bookDto, imageUrl);

                return Ok("Book added successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("UpdateBook")]
        public async Task<IActionResult> UpdateBook([FromForm] UpdateBookDTO bookDto)
        {
            try
            {
                if (bookDto == null)
                {
                    return BadRequest("Book data is null.");
                }

                string imageUrl = null;

                // If you allow updating the cover image:
                if (bookDto.CoverImage != null && bookDto.CoverImage.Length > 0)
                {
                    var imageService = HttpContext.RequestServices.GetService(typeof(EBookNepal.Services.ImageServices)) as EBookNepal.Services.ImageServices;
                    var uploadResult = await imageService.UploadPhotoAsync(bookDto.CoverImage);
                    imageUrl = uploadResult.SecureUrl?.ToString();
                }

                _bookServices.UpdateBook(bookDto, imageUrl);
                return Ok("Book updated successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("AddToWishlist")]
        public IActionResult AddToWishlist([FromQuery] string userId, [FromQuery] string bookId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("User ID is required.");
                }

                if (string.IsNullOrEmpty(bookId))
                {
                    return BadRequest("Book ID is required.");
                }

                _bookServices.AddToWishlist(userId, bookId);
                return Ok("Book added to wishlist successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetWishlist")]
        public IActionResult GetWishlist([FromQuery] string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("User ID is required.");
                }

                var wishlist = _bookServices.GetWishlist(userId);

                if (wishlist == null || !wishlist.Any())
                {
                    return NotFound("No wishlist items found for the user.");
                }

                return Ok(wishlist);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("RemoveFromWishlist")]
        public IActionResult RemoveFromWishlist([FromQuery] string wishlistId)
        {
            try
            {
                if (string.IsNullOrEmpty(wishlistId))
                {
                    return BadRequest("Wishlist ID is required.");
                }

                _bookServices.RemoveFromWishlist(wishlistId);
                return Ok("Book removed from wishlist successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("SetOffer")]
        public IActionResult SetOffer([FromBody] SetOfferDTO offerDto)
        {
            try
            {
                if (offerDto == null)
                {
                    return BadRequest("Offer data is required.");
                }

                _bookServices.SetOffer(offerDto);
                return Ok("Offer price set successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetPopularBooks")]
        public IActionResult GetPopularBooks()
        {
            try
            {
                var popularBooks = _bookServices.GetPopularBooks();
                return Ok(popularBooks);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving popular books: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving popular books.");
            }
        }

        [HttpGet("GetOnSaleBooks")]
        public IActionResult GetOnSaleBooks()
        {
            try
            {
                var onSaleBooks = _bookServices.GetOnSaleBooks();
                return Ok(onSaleBooks);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving on-sale books: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving on-sale books.");
            }
        }

        [HttpPost("AddReview")]
        public IActionResult AddReview([FromBody] AddReviewDTO reviewDto)
        {
            try
            {
                if (reviewDto == null || string.IsNullOrEmpty(reviewDto.BookId) || string.IsNullOrEmpty(reviewDto.UserId))
                {
                    return BadRequest("Book ID and User ID are required.");
                }

                // Check if the user has purchased the book
                var hasPurchased = _context.Orders
                    .Include(o => o.OrderItems)
                    .Any(o => o.UserId == reviewDto.UserId &&
                            o.OrderStatus == OrderStatus.Completed &&
                            o.OrderItems.Any(oi => oi.BookId == reviewDto.BookId));

                if (!hasPurchased)
                {
                    return BadRequest("You can only review books that you have purchased.");
                }

                // Add the review
                var review = new BookReview
                {
                    BookId = reviewDto.BookId,
                    UserId = reviewDto.UserId,
                    ReviewText = reviewDto.ReviewText,
                    Rating = reviewDto.Rating
                };

                _context.BookReviews.Add(review);
                _context.SaveChanges();

                return Ok("Review added successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"An error occurred: {ex.Message}");
            }
        }
    }
}