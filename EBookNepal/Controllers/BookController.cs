using EBookNepal.Data;
using EBookNepal.DTOS;
using EBookNepal.Entities;
using EBookNepal.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<User> _userManager;
        private readonly IImageServices _imageService;

        public BookController(
            IBookServices bookServices,
            IWebHostEnvironment webHostEnvironment,
            ILogger<BookController> logger,
            ApplicationDbContext context,
            UserManager<User> userManager,
            IImageServices imageService)
        {
            _bookServices = bookServices;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _imageService = imageService; // ✅ was missing
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
        // GET BOOKS BY SELLER ID
        // ======================================================
        [HttpGet("GetBooksBySeller")]
        public IActionResult GetBooksBySeller([FromQuery] string sellerId)
        {
            if (string.IsNullOrWhiteSpace(sellerId))
                return BadRequest("Seller ID is required.");

            try
            {
                var books = _bookServices.GetBooksBySeller(sellerId);
                return Ok(books);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving books for seller {SellerId}", sellerId);
                return BadRequest(ex.Message);
            }
        }

        // ======================================================
        // GET MY BOOKS (authenticated seller's own books)
        // ======================================================
        [HttpGet("GetMyBooks")]
        [Authorize]
        public IActionResult GetMyBooks()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("User not found.");

            try
            {
                var books = _bookServices.GetBooksBySeller(userId);
                return Ok(books);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving books for current user");
                return BadRequest(ex.Message);
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

            try
            {
                var book = _bookServices.GetBookById(bookId);
                return Ok(book);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
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
        [Authorize]
        public async Task<IActionResult> AddBook([FromForm] AddBookDTO bookDto)
        {
            if (bookDto == null)
                return BadRequest("Book data is null.");

            string imageUrl = null;

            if (bookDto.CoverImage != null)
            {
                var uploadResult = await _imageService.UploadPhotoAsync(bookDto.CoverImage); // ✅ injected service
                if (uploadResult?.SecureUrl == null)
                    return BadRequest("Cover image upload failed. Please try again.");

                imageUrl = uploadResult.SecureUrl.ToString();
            }

            try
            {
                _bookServices.AddBook(bookDto, imageUrl);
                return Ok("Book added successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding book");
                return BadRequest(ex.Message);
            }
        }

        // ======================================================
        // UPDATE BOOK
        // ======================================================
        [HttpPatch("UpdateBook")]
        [Authorize]
        public async Task<IActionResult> UpdateBook([FromForm] UpdateBookDTO bookDto)
        {
            if (bookDto == null)
                return BadRequest("Book data is null.");

            string imageUrl = null;

            if (bookDto.CoverImage != null)
            {
                var uploadResult = await _imageService.UploadPhotoAsync(bookDto.CoverImage); // ✅ injected service
                if (uploadResult?.SecureUrl == null)
                    return BadRequest("Cover image upload failed. Please try again.");

                imageUrl = uploadResult.SecureUrl.ToString();
            }

            try
            {
                _bookServices.UpdateBook(bookDto, imageUrl);
                return Ok("Book updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating book");
                return BadRequest(ex.Message);
            }
        }

        // ======================================================
        // DELETE BOOK
        // ======================================================
        [HttpDelete("DeleteBook")]
        [Authorize]
        public IActionResult DeleteBook([FromQuery] string bookId)
        {
            if (string.IsNullOrWhiteSpace(bookId))
                return BadRequest("Book ID is required.");

            try
            {
                _bookServices.DeleteBook(bookId);
                return Ok("Book deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting book {BookId}", bookId);
                return BadRequest(ex.Message);
            }
        }

        // ======================================================
        // WISHLIST
        // ======================================================
        [HttpPost("AddToWishlist")]
        [Authorize]
        public IActionResult AddToWishlist([FromQuery] string bookId)
        {
            if (string.IsNullOrWhiteSpace(bookId))
                return BadRequest("Book ID is required.");

            try
            {
                _bookServices.AddToWishlist(bookId);
                return Ok("Book added to wishlist.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetWishlist")]
        [Authorize]
        public IActionResult GetWishlist()
        {
            var wishlist = _bookServices.GetWishlist();
            return Ok(wishlist);
        }

        [HttpDelete("RemoveFromWishlist")]
        [Authorize]
        public IActionResult RemoveFromWishlist([FromQuery] string wishlistId)
        {
            if (string.IsNullOrWhiteSpace(wishlistId))
                return BadRequest("Wishlist ID is required.");

            try
            {
                _bookServices.RemoveFromWishlist(wishlistId);
                return Ok("Removed from wishlist.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ======================================================
        // CART
        // ======================================================
        [HttpPost("AddToCart")]
        [Authorize]
        public IActionResult AddToCart([FromQuery] string bookId, [FromQuery] int quantity = 1)
        {
            if (string.IsNullOrWhiteSpace(bookId))
                return BadRequest("Book ID is required.");

            try
            {
                _bookServices.AddToCart(bookId, quantity);
                return Ok("Book added to cart.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetCart")]
        [Authorize]
        public IActionResult GetCart()
        {
            var cart = _bookServices.GetCart();
            return Ok(cart);
        }

        [HttpDelete("RemoveFromCart")]
        [Authorize]
        public IActionResult RemoveFromCart([FromQuery] string cartItemId)
        {
            if (string.IsNullOrWhiteSpace(cartItemId))
                return BadRequest("Cart item ID is required.");

            try
            {
                _bookServices.RemoveFromCart(cartItemId);
                return Ok("Removed from cart.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ======================================================
        // OFFERS
        // ======================================================
        [HttpPost("SetOffer")]
        public IActionResult SetOffer([FromBody] SetOfferDTO offerDto)
        {
            if (offerDto == null)
                return BadRequest("Offer data is required.");

            try
            {
                _bookServices.SetOffer(offerDto);
                return Ok("Offer applied successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
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
        [Authorize]
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