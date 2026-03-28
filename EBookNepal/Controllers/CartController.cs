namespace EBookNepal.Controllers
{
    using EBookNepal.Entities;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Hosting;
    using System;
    using System.Linq;
    using EBookNepal.Services.Interfaces;
    using EBookNepal.Data;
    using Microsoft.EntityFrameworkCore;
    using EBookNepal.DTOS;

    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly IBookServices _bookServices;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ICartServices _cartService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartController> _logger;
        private readonly IEmailServices _emailService;
        public CartController(IBookServices bookServices, IWebHostEnvironment webHostEnvironment, ICartServices cartService, ApplicationDbContext context, ILogger<CartController> logger, IEmailServices emailService)
        {
            _bookServices = bookServices;
            _webHostEnvironment = webHostEnvironment;
            _cartService = cartService;
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        [HttpGet("GetOrdersBySeller/{sellerId}")]
        public IActionResult GetOrdersBySeller(string sellerId)
        {
            try
            {
                if (string.IsNullOrEmpty(sellerId))
                    return BadRequest("Seller ID is required.");

                var orders = _context.OrderItems
                    .Include(oi => oi.Order)
                    .Where(oi => oi.SellerId == sellerId)
                    .Select(oi => new
                    {
                        oi.Order.OrderId,
                        oi.Order.TotalAmount,
                        oi.Order.CheckedOutTime,
                        oi.Order.OrderStatus,
                        OrderItem = new
                        {
                            oi.OrderItemId,
                            oi.BookId,
                            oi.BookTitle,
                            oi.BookPrice,
                            oi.Quantity,
                            oi.TotalPrice,
                            oi.SellerId
                        }
                    })
                    .ToList()
                    .GroupBy(x => x.OrderId)
                    .Select(g => new
                    {
                        OrderId = g.Key,
                        TotalAmount = g.First().TotalAmount,
                        CheckedOutTime = g.First().CheckedOutTime,
                        OrderStatus = g.First().OrderStatus,
                        OrderItems = g.Select(x => x.OrderItem).ToList()
                    })
                    .ToList();

                if (!orders.Any())
                    return NotFound("No orders found for the specified seller.");

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving orders for seller {sellerId}: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving the orders.");
            }
        }

        [HttpGet("GetBooks")]
        public IEnumerable<Book> GetBooks()
        {
            try
            {
                return _context.Books.AsNoTracking().ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving books: " + ex.Message);
            }
        }

        [HttpPost("AddToCart/{userId}")]
        public IActionResult AddToCart(string userId, [FromBody] AddToCartDTO cartItem)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("User ID is required.");
                }

                if (cartItem == null)
                {
                    return BadRequest("Cart item data is null.");
                }

                // Check if the book exists
                var book = _bookServices.GetBooks().FirstOrDefault(b => b.BookId == cartItem.BookId);
                if (book == null)
                {
                    return NotFound("Book not found.");
                }

                // Add the cart item to the database
                _cartService.AddToCart(userId, cartItem);

                return Ok("Book added to cart successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpGet("ViewCart/{userId}")]
        public IActionResult ViewCart(string userId)
        {
            try
            {
                Console.WriteLine($"ViewCart - UserId: {userId}");
                var cartItems = _cartService.GetCartItemsByUserId(userId);
                if (!cartItems.Any())
                {
                    return NotFound("No items found in the cart.");
                }

                return Ok(cartItems);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ViewCart: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("UpdateCartItem")]
        public IActionResult UpdateCartItem([FromBody] UpdateCartDTO dto)
        {
            if (dto == null)
                return BadRequest("Cart item data is null.");

            try
            {
                _cartService.UpdateCartItem(dto);
                return Ok("Cart item updated successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("RemoveFromCart/{cartItemId}")]
        public IActionResult RemoveFromCart(string cartItemId)
        {
            try
            {
                _cartService.RemoveFromCart(cartItemId);

                return Ok("Cart item removed successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("ClearCart/{userId}")]
        public IActionResult ClearCart(string userId)
        {
            try
            {
                _cartService.ClearCart(userId);

                return Ok("Cart cleared successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetTotalPrice/{userId}")]
        public IActionResult GetTotalPrice(string userId)
        {
            try
            {
                var totalPrice = _cartService.GetTotalPrice(userId);

                return Ok(new { TotalPrice = totalPrice });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("Checkout/{userId}")]
        public async Task<IActionResult> Checkout(string userId)
        {
            try
            {
                var cartItems = _cartService.GetCartItemsByUserId(userId);
                if (!cartItems.Any())
                {
                    return NotFound("No items found in the cart.");
                }

                // ==============================
                // CHECK PREVIOUS COMPLETED ORDERS
                // ==============================
                var successfulOrdersCount = _context.Orders
                    .Where(o => o.UserId == userId && o.OrderStatus == OrderStatus.Completed)
                    .Count();

                bool applyExtraDiscount = (successfulOrdersCount + 1) % 11 == 0;

                decimal totalAmount = 0;

                // ==============================
                // VALIDATE STOCK FIRST
                // ==============================
                foreach (var cartItem in cartItems)
                {
                    var book = _context.Books.FirstOrDefault(b => b.BookId == cartItem.BookId);
                    if (book == null)
                        return BadRequest($"Book not found: {cartItem.BookId}");

                    if (book.Stock < cartItem.Quantity)
                        return BadRequest($"Not enough stock for {book.Title}");
                }

                // ==============================
                // CREATE ORDER
                // ==============================
                var claimCode = Guid.NewGuid().ToString();

                var order = new Order
                {
                    OrderId = Guid.NewGuid().ToString(),
                    UserId = userId,
                    CheckedOutTime = DateTime.UtcNow,
                    OrderStatus = OrderStatus.Pending,
                    ClaimCode = claimCode
                };

                _context.Orders.Add(order);

                // ==============================
                // PROCESS ITEMS + DEDUCT STOCK
                // ==============================
                foreach (var cartItem in cartItems)
                {
                    var book = _context.Books.FirstOrDefault(b => b.BookId == cartItem.BookId);

                    decimal itemPrice = cartItem.UnitPrice;

                    if (cartItem.Quantity > 5)
                        itemPrice *= 0.95m;

                    totalAmount += itemPrice * cartItem.Quantity;

                    // Deduct inventory
                    book.Stock -= cartItem.Quantity;

                    var orderItem = new OrderItem
                    {
                        OrderItemId = Guid.NewGuid().ToString(),
                        OrderId = order.OrderId,
                        BookId = cartItem.BookId,
                        SellerId = book.SellerId,
                        BookTitle = book.Title,
                        BookPrice = itemPrice,
                        Quantity = cartItem.Quantity,
                        TotalPrice = itemPrice * cartItem.Quantity
                    };

                    _context.OrderItems.Add(orderItem);
                }

                // ==============================
                // APPLY LOYALTY DISCOUNT
                // ==============================
                if (applyExtraDiscount)
                {
                    totalAmount *= 0.90m;
                }

                order.TotalAmount = totalAmount;

                // ==============================
                // SAVE ORDER + STOCK
                // ==============================
                _context.SaveChanges();

                // ==============================
                // CLEAR CART AFTER SUCCESS
                // ==============================
                _cartService.ClearCart(userId);

                // ==============================
                // SEND EMAIL
                // ==============================
                var userEmail = _context.Users.FirstOrDefault(u => u.Id == userId)?.Email;
                if (string.IsNullOrEmpty(userEmail))
                    return BadRequest("User email not found.");

                var emailSubject = "Order Confirmation - EBookNepal";

                var emailBody = $"Dear User,\n\n" +
                                $"Thank you for your order.\n\n" +
                                $"Order ID: {order.OrderId}\n" +
                                $"Claim Code: {claimCode}\n" +
                                $"Total Amount: {totalAmount:C}\n\n" +
                                $"Items:\n";

                foreach (var cartItem in cartItems)
                {
                    emailBody += $"- {cartItem.Quantity} x {cartItem.BookTitle}\n";
                }

                if (applyExtraDiscount)
                {
                    emailBody += "\nYou received a 10% loyalty discount.\n";
                }

                emailBody += "\nThank you for shopping with EBookNepal.";

                await _emailService.SendEmailAsync(userEmail, emailSubject, emailBody);

                return Ok("Checkout completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Checkout failed: {ex.Message}");
                return StatusCode(500, "An error occurred during checkout.");
            }
        }

        [HttpGet("ViewOrders/{userId}")]
        public IActionResult ViewOrdersByUserId(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("User ID is required.");
                }

                var orders = _context.Orders
                    .Include(o => o.OrderItems) // Include related OrderItems
                    .Where(o => o.UserId == userId)
                    .Select(order => new
                    {
                        OrderId = order.OrderId,
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
                    return NotFound("No orders found for the specified user.");
                }

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving orders for user {userId}: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving the orders.");
            }
        }

        [HttpPut("CancelOrder/{orderId}")]
        public IActionResult CancelOrder(string orderId)
        {
            try
            {
                if (string.IsNullOrEmpty(orderId))
                {
                    return BadRequest("Order ID is required.");
                }

                var order = _context.Orders.FirstOrDefault(o => o.OrderId == orderId);

                if (order == null)
                {
                    return NotFound("Order not found.");
                }

                if (order.OrderStatus == OrderStatus.Completed)
                {
                    return BadRequest("Completed orders cannot be cancelled.");
                }

                if (order.OrderStatus == OrderStatus.Cancelled)
                {
                    return BadRequest("Order is already cancelled.");
                }

                // Update the order status to Cancelled
                order.OrderStatus = OrderStatus.Cancelled;

                _context.Orders.Update(order);
                _context.SaveChanges();

                return Ok("Order has been cancelled successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cancelling order: {ex.Message}");
                return StatusCode(500, "An error occurred while cancelling the order.");
            }
        }
    }
}