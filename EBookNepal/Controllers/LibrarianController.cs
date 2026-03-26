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

        public LibrarianController(IBookServices bookServices, IWebHostEnvironment webHostEnvironment, ILogger<LibrarianController> logger, ApplicationDbContext context)
        {
            _bookServices = bookServices;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _context = context;
        }

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

        [HttpPut("UpdateOrderStatus")]
        public IActionResult UpdateOrderStatus([FromBody] UpdateOrderStatusDTO updateOrderStatusDTO)
        {
            try
            {
                if (updateOrderStatusDTO == null || string.IsNullOrEmpty(updateOrderStatusDTO.OrderId) || string.IsNullOrEmpty(updateOrderStatusDTO.ClaimCode))
                {
                    return BadRequest("Order ID, claim code, and status details are required.");
                }

                var order = _context.Orders
                    .Include(o => o.OrderItems) // Include related OrderItems
                    .FirstOrDefault(o => o.OrderId == updateOrderStatusDTO.OrderId);

                if (order == null)
                {
                    return NotFound("Order not found.");
                }

                // Validate the claim code
                if (!string.Equals(order.ClaimCode, updateOrderStatusDTO.ClaimCode, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest("Invalid claim code.");
                }

                // Update the order status
                order.OrderStatus = updateOrderStatusDTO.OrderStatus;

                // If the order status is updated to 'Completed', decrease the stock
                if (updateOrderStatusDTO.OrderStatus == OrderStatus.Completed)
                {
                    foreach (var orderItem in order.OrderItems)
                    {
                        var book = _context.Books.FirstOrDefault(b => b.BookId == orderItem.BookId);
                        if (book == null)
                        {
                            return BadRequest($"Book with ID {orderItem.BookId} not found.");
                        }

                        // Decrease the stock
                        if (book.Stock < orderItem.Quantity)
                        {
                            return BadRequest($"Insufficient stock for book '{book.Title}'.");
                        }

                        book.Stock -= orderItem.Quantity;
                        _context.Books.Update(book);
                    }
                }

                _context.Orders.Update(order);
                _context.SaveChanges();

                return Ok("Order status updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating order status: {ex.Message}");
                return BadRequest("An error occurred while updating the order status.");
            }
        }

        [HttpGet("GetOrders")]
        public IActionResult GetOrders()
        {
            try
            {
                var orders = _context.Orders
                    .Include(o => o.OrderItems)
                    .Select(order => new
                    {
                        OrderId = order.OrderId,
                        UserId = order.UserId,
                        TotalAmount = order.TotalAmount,
                        CheckedOutTime = order.CheckedOutTime,
                        OrderStatus = order.OrderStatus,
                        OrderItems = order.OrderItems.Select(item => new
                        {
                            OrderItemId = item.OrderItemId,
                            BookId = item.BookId,
                            BookTitle = item.BookTitle,
                            BookPrice = item.BookPrice,
                            Quantity = item.Quantity,
                            TotalPrice = item.TotalPrice
                        }).ToList()
                    })
                    .ToList();

                if (!orders.Any())
                {
                    return NotFound("No orders found.");
                }

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving orders: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving the orders.");
            }
        }

        [HttpGet("GetBookReviews")]
        public IActionResult GetBookReviews([FromQuery] string bookId)
        {
            try
            {
                if (string.IsNullOrEmpty(bookId))
                {
                    return BadRequest("Book ID is required.");
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

                if (!reviews.Any())
                {
                    return NotFound("No reviews found for this book.");
                }

                return Ok(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving reviews: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving reviews.");
            }
        }
    }
}