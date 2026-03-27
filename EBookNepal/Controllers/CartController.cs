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
        public IActionResult UpdateCartItem([FromBody] Cart cartItem)
        {
            try
            {
                if (cartItem == null)
                {
                    return BadRequest("Cart item data is null.");
                }

                _cartService.UpdateCartItem(cartItem);

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

                // Check if the user has 10 successful orders
                var successfulOrdersCount = _context.Orders
                    .Where(o => o.UserId == userId && o.OrderStatus == OrderStatus.Completed) // Assuming 1 = Successful
                    .Count();

                bool applyExtraDiscount = (successfulOrdersCount + 1) % 11 == 0; // Apply extra discount on every 11th order

                // Calculate total amount with discount logic
                decimal totalAmount = 0;
                foreach (var cartItem in cartItems)
                {
                    decimal itemPrice = cartItem.UnitPrice;
                    if (cartItem.Quantity > 5)
                    {
                        // Apply 5% discount for quantity > 5
                        itemPrice *= 0.95m;
                    }
                    totalAmount += itemPrice * cartItem.Quantity;
                }

                // Apply extra 10% discount if eligible
                if (applyExtraDiscount)
                {
                    totalAmount *= 0.90m; // Apply 10% discount
                }

                // Generate a claim code
                var claimCode = Guid.NewGuid().ToString();

                // Create a new order
                var order = new Order
                {
                    OrderId = Guid.NewGuid().ToString(),
                    UserId = userId,
                    TotalAmount = totalAmount,
                    CheckedOutTime = DateTime.UtcNow,
                    OrderStatus = OrderStatus.Pending,
                    ClaimCode = claimCode
                };

                _context.Orders.Add(order);

                // Add order items
                foreach (var cartItem in cartItems)
                {
                    var book = _context.Books.FirstOrDefault(b => b.BookId == cartItem.BookId);
                    if (book == null)
                    {
                        return BadRequest($"Book with ID {cartItem.BookId} not found.");
                    }

                    decimal itemPrice = cartItem.UnitPrice;
                    if (cartItem.Quantity > 5)
                    {
                        // Apply 5% discount for quantity > 5
                        itemPrice *= 0.95m;
                    }

                    var orderItem = new OrderItem
                    {
                        OrderId = order.OrderId,
                        BookId = cartItem.BookId,
                        BookTitle = book.Title,
                        BookPrice = itemPrice, // Discounted price
                        Quantity = cartItem.Quantity,
                        TotalPrice = itemPrice * cartItem.Quantity
                    };

                    _context.OrderItems.Add(orderItem);
                }

                // Clear the cart
                _cartService.ClearCart(userId);

                // Save changes to the database
                _context.SaveChanges();

                // Send email with order details and claim code
                var userEmail = _context.Users.FirstOrDefault(u => u.Id == userId)?.Email;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return BadRequest("User email not found.");
                }

                var emailSubject = "Order Confirmation - EBookNepal";
                var emailBody = $"Dear User,\n\nThank you for your order. Here are the details:\n\n" +
                                $"Order ID: {order.OrderId}\n" +
                                $"Total Amount: {totalAmount:C}\n" +
                                $"Checked Out Time: {order.CheckedOutTime}\n" +
                                $"Claim Code: {claimCode}\n\n" +
                                $"Order Items:\n";

                foreach (var cartItem in cartItems)
                {
                    decimal itemPrice = cartItem.UnitPrice;
                    if (cartItem.Quantity > 5)
                    {
                        itemPrice *= 0.95m;
                    }

                    emailBody += $"- {cartItem.Quantity} x {cartItem.BookTitle} @ {itemPrice:C} each\n";
                }

                emailBody += $"\nTotal: {totalAmount:C}\n\n";

                if (applyExtraDiscount)
                {
                    emailBody += "Congratulations! You received an extra 10% discount for completing 10 successful orders.\n\n";
                }

                emailBody += "Please keep this email for your records. Use the claim code to verify your order.\n\n" +
                            "Thank you,\nEBookNepal Team";

                await _emailService.SendEmailAsync(userEmail, emailSubject, emailBody);

                return Ok("Checkout completed successfully. Orders have been created, and an email has been sent.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
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