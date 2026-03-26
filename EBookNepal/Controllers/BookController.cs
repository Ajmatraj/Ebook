using EBookNepal.Data;
using EBookNepal.DTOS;
using EBookNepal.Entities;
using EBookNepal.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EBookNepal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookController : ControllerBase
    {
        private readonly IBookServices _bookServices;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<BookController> _logger;
        private readonly ApplicationDbContext _context;

        public BookController(
            IBookServices bookServices,
            IWebHostEnvironment webHostEnvironment,
            ILogger<BookController> logger,
            ApplicationDbContext context)
        {
            _bookServices = bookServices;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _context = context;
        }

        // ======================================================
        // GET ALL BOOKS
        // ======================================================
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
                _logger.LogError(ex, "Error retrieving books");
                return BadRequest("Error retrieving books.");
            }
        }

        // ======================================================
        // GET BOOK BY ID
        // ======================================================
        [HttpGet("GetBookById")]
        public IActionResult GetBookById([FromQuery] string bookId)
        {
            if (string.IsNullOrWhiteSpace(bookId))
                return BadRequest("Book ID is required.");

            var book = _context.Books
                .Where(b => b.BookId == bookId)
                .Select(b => new BookDTO
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Stock = b.Stock,
                    Description = b.Description,
                    Author = b.Author,
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
                    CoverImagePath = b.CoverImageUrl
                })
                .FirstOrDefault();

            if (book == null)
                return NotFound("Book not found.");

            return Ok(book);
        }

        // ======================================================
        // FILTER BOOKS
        // ======================================================
        [HttpGet("FilterBooks")]
        public IActionResult FilterBooks([FromQuery] FilterBookDTO filter)
        {
            var books = _bookServices.GetBooks();

            if (!string.IsNullOrWhiteSpace(filter.Title))
                books = books.Where(b => b.Title.Contains(filter.Title, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(filter.Genre))
                books = books.Where(b => b.Genre.Contains(filter.Genre, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(filter.Author))
                books = books.Where(b => b.Author.Contains(filter.Author, StringComparison.OrdinalIgnoreCase));

            if (filter.PublicationDate.HasValue)
            {
                books = books.Where(b =>
                    DateTime.TryParse(b.PublicationDate, out var parsedDate) &&
                    parsedDate.Date == filter.PublicationDate.Value.Date);
            }

            return Ok(books);
        }

        // ======================================================
        // ADD BOOK
        // ======================================================
        [HttpPost("AddBook")]
        public async Task<IActionResult> AddBook([FromForm] AddBookDTO bookDto)
        {
            if (bookDto == null)
                return BadRequest("Book data is null.");

            string imageUrl = null;

            if (bookDto.CoverImage != null)
            {
                var imageService = HttpContext.RequestServices
                    .GetService(typeof(EBookNepal.Services.ImageServices))
                        as EBookNepal.Services.ImageServices;

                if (imageService != null)
                {
                    var uploadResult = await imageService.UploadPhotoAsync(bookDto.CoverImage);
                    imageUrl = uploadResult?.SecureUrl?.ToString();
                }
            }

            _bookServices.AddBook(bookDto, imageUrl);

            return Ok("Book added successfully.");
        }

        // ======================================================
        // UPDATE BOOK
        // ======================================================
        [HttpPost("UpdateBook")]
        public async Task<IActionResult> UpdateBook([FromForm] UpdateBookDTO bookDto)
        {
            if (bookDto == null)
                return BadRequest("Book data is null.");

            string imageUrl = null;

            if (bookDto.CoverImage != null)
            {
                var imageService = HttpContext.RequestServices
                    .GetService(typeof(EBookNepal.Services.ImageServices))
                        as EBookNepal.Services.ImageServices;

                if (imageService != null)
                {
                    var uploadResult = await imageService.UploadPhotoAsync(bookDto.CoverImage);
                    imageUrl = uploadResult?.SecureUrl?.ToString();
                }
            }

            _bookServices.UpdateBook(bookDto, imageUrl);

            return Ok("Book updated successfully.");
        }

        // ======================================================
        // WISHLIST
        // ======================================================
        [HttpPost("AddToWishlist")]
        public IActionResult AddToWishlist([FromQuery] string bookId)
        {
            if (string.IsNullOrWhiteSpace(bookId))
                return BadRequest("Book ID is required.");

            _bookServices.AddToWishlist(bookId);

            return Ok("Book added to wishlist.");
        }

        [HttpGet("GetWishlist")]
        public IActionResult GetWishlist()
        {
            var wishlist = _bookServices.GetWishlist();
            return Ok(wishlist);
        }

        [HttpDelete("RemoveFromWishlist")]
        public IActionResult RemoveFromWishlist([FromQuery] string wishlistId)
        {
            if (string.IsNullOrWhiteSpace(wishlistId))
                return BadRequest("Wishlist ID is required.");

            _bookServices.RemoveFromWishlist(wishlistId);

            return Ok("Removed from wishlist.");
        }

        // ======================================================
        // OFFERS
        // ======================================================
        [HttpPost("SetOffer")]
        public IActionResult SetOffer([FromBody] SetOfferDTO offerDto)
        {
            if (offerDto == null)
                return BadRequest("Offer data is required.");

            _bookServices.SetOffer(offerDto);

            return Ok("Offer applied successfully.");
        }

        // ======================================================
        // POPULAR BOOKS
        // ======================================================
        [HttpGet("GetPopularBooks")]
        public IActionResult GetPopularBooks()
        {
            var popularBooks = _bookServices.GetPopularBooks();
            return Ok(popularBooks);
        }

        // ======================================================
        // ON SALE BOOKS
        // ======================================================
        [HttpGet("GetOnSaleBooks")]
        public IActionResult GetOnSaleBooks()
        {
            var onSaleBooks = _bookServices.GetOnSaleBooks();
            return Ok(onSaleBooks);
        }

        // ======================================================
        // ADD REVIEW
        // ======================================================
        [HttpPost("AddReview")]
        public IActionResult AddReview([FromBody] AddReviewDTO reviewDto)
        {
            if (reviewDto == null ||
                string.IsNullOrWhiteSpace(reviewDto.BookId) ||
                string.IsNullOrWhiteSpace(reviewDto.UserId))
            {
                return BadRequest("Book ID and User ID are required.");
            }

            var hasPurchased = _context.Orders
                .Include(o => o.OrderItems)
                .Any(o => o.UserId == reviewDto.UserId &&
                          o.OrderStatus == OrderStatus.Completed &&
                          o.OrderItems.Any(i => i.BookId == reviewDto.BookId));

            if (!hasPurchased)
                return BadRequest("You can only review books you have purchased.");

            var review = new BookReview
            {
                ReviewId = Guid.NewGuid().ToString(),
                BookId = reviewDto.BookId,
                UserId = reviewDto.UserId,
                ReviewText = reviewDto.ReviewText,
                Rating = reviewDto.Rating,
                CreatedAt = DateTime.UtcNow
            };

            _context.BookReviews.Add(review);
            _context.SaveChanges();

            return Ok("Review added successfully.");
        }
    }
}