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
    public class LibrarianController : ControllerBase
    {
        private readonly IBookServices _bookServices;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<LibrarianController> _logger;
        private readonly ApplicationDbContext _context;

        public LibrarianController(
            IBookServices bookServices,
            IWebHostEnvironment webHostEnvironment,
            ILogger<LibrarianController> logger,
            ApplicationDbContext context)
        {
            _bookServices = bookServices;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _context = context;
        }

        // ============================================================
        // GET BOOKS
        // ============================================================
        [HttpGet("GetBooks")]
        public IEnumerable<BookDTO> GetBooks()
        {
            return _context.Books.Select(book => new BookDTO
            {
                BookId = book.BookId,
                Title = book.Title,
                Author = book.Author,
                Genre = book.Genre,
                Language = book.Language,
                ISBN = book.ISBN,
                Publisher = book.Publisher,
                PublicationDate = book.PublicationDate.HasValue
                    ? book.PublicationDate.Value.ToString("yyyy-MM-dd")
                    : null,
                Price = (book.OfferPrice.HasValue &&
                         book.OfferStartDate <= DateTime.UtcNow &&
                         book.OfferEndDate >= DateTime.UtcNow)
                        ? book.OfferPrice.Value
                        : book.Price,
                CoverImagePath = book.CoverImageUrl
            }).ToList();
        }

        // ============================================================
        // UPDATE ORDER STATUS
        // ============================================================
        [HttpPut("UpdateOrderStatus")]
        public IActionResult UpdateOrderStatus([FromBody] UpdateOrderStatusDTO dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Request body is missing." });
                }

                if (string.IsNullOrWhiteSpace(dto.OrderId) ||
                    string.IsNullOrWhiteSpace(dto.ClaimCode))
                {
                    return BadRequest(new { message = "OrderId and ClaimCode are required." });
                }

                var order = _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefault(o => o.OrderId == dto.OrderId);

                if (order == null)
                {
                    return NotFound(new { message = "Order not found." });
                }

                // ================= CLAIM CODE CHECK =================
                if (!string.Equals(
                        order.ClaimCode?.Trim(),
                        dto.ClaimCode?.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        $"Invalid claim code. DB: '{order.ClaimCode}', Incoming: '{dto.ClaimCode}'");

                    return BadRequest(new { message = "Invalid claim code." });
                }

                // ================= UPDATE STATUS =================
                order.OrderStatus = dto.OrderStatus;

                // ================= STOCK DEDUCTION =================
                if (dto.OrderStatus == OrderStatus.Completed)
                {
                    var bookIds = order.OrderItems.Select(i => i.BookId).ToList();

                    var books = _context.Books
                        .Where(b => bookIds.Contains(b.BookId))
                        .ToDictionary(b => b.BookId);

                    foreach (var item in order.OrderItems)
                    {
                        if (!books.TryGetValue(item.BookId, out var book))
                        {
                            return BadRequest(new { message = $"Book not found for ID {item.BookId}" });
                        }

                        if (book.Stock < item.Quantity)
                        {
                            return BadRequest(new
                            {
                                message = $"Insufficient stock for '{book.Title}'."
                            });
                        }

                        book.Stock -= item.Quantity;
                    }
                }

                _context.SaveChanges();

                return Ok(new
                {
                    message = "Order status updated successfully.",
                    orderId = order.OrderId,
                    newStatus = order.OrderStatus.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // ============================================================
        // GET ORDERS (FIXED - NOW RETURNS CLAIM CODE)
        // ============================================================
        [HttpGet("GetOrders")]
        public IActionResult GetOrders()
        {
            try
            {
                var orders = _context.Orders
                    .Include(o => o.OrderItems)
                    .Select(order => new
                    {
                        order.OrderId,
                        order.UserId,
                        order.TotalAmount,
                        order.CheckedOutTime,
                        order.OrderStatus,
                        order.ClaimCode,   
                        OrderItems = order.OrderItems.Select(item => new
                        {
                            item.OrderItemId,
                            item.BookId,
                            item.BookTitle,
                            item.BookPrice,
                            item.Quantity,
                            item.TotalPrice
                        }).ToList()
                    })
                    .ToList();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders");
                return StatusCode(500, new { message = "Failed to retrieve orders." });
            }
        }

        // ============================================================
        // GET BOOK REVIEWS
        // ============================================================
        [HttpGet("GetBookReviews")]
        public IActionResult GetBookReviews([FromQuery] string bookId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(bookId))
                {
                    return BadRequest(new { message = "Book ID is required." });
                }

                var reviews = _context.BookReviews
                    .Where(r => r.BookId == bookId)
                    .Select(r => new
                    {
                        r.ReviewId,
                        r.UserId,
                        r.ReviewText,
                        r.Rating,
                        r.CreatedAt
                    })
                    .ToList();

                return Ok(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reviews");
                return StatusCode(500, new { message = "Failed to retrieve reviews." });
            }
        }
    }
}