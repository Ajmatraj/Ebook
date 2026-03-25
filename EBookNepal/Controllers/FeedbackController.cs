namespace EBookNepal.Controllers
{
    using EBookNepal.DTOS;
    using EBookNepal.Services.Interfaces;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [Route("api/[controller]")]
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackServices _feedbackServices;
        private readonly ILogger<FeedbackController> _logger;
        private readonly IEmailServices _emailService;
        private readonly IConfiguration _configuration;

        public FeedbackController(
            IFeedbackServices feedbackServices,
            ILogger<FeedbackController> logger,
            IEmailServices emailService,
            IConfiguration configuration)
        {
            _feedbackServices = feedbackServices;
            _logger = logger;
            _emailService = emailService;
            _configuration = configuration;
        }

        [HttpPost("SendFeedback")]
        public async Task<IActionResult> SendFeedback([FromBody] FeedbackDTO feedbackDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Invalid feedback data.");
                }

                // Save feedback using the service
                _feedbackServices.SendFeedback(feedbackDTO);

                // Send email to admin
                var adminEmail = "raajajmat252@gmail.com";
                var emailSubject = $"New Feedback from {feedbackDTO.Email}";
                var emailBody = $"Subject: {feedbackDTO.Subject}\n\nMessage:\n{feedbackDTO.Message}";

                await _emailService.SendEmailAsync(adminEmail, emailSubject, emailBody);

                return Ok("Feedback sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending feedback: {ex.Message}");
                return StatusCode(500, "An error occurred while sending feedback.");
            }
        }

        [HttpGet("GetFeedbacks")]
        public IActionResult GetFeedbacks()
        {
            try
            {
                var feedbacks = _feedbackServices.GetFeedbacks();
                return Ok(feedbacks);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving feedbacks: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving feedbacks.");
            }
        }

        //repali to user by email.
        [HttpPost("ReplyFeedback")]
        public async Task<IActionResult> ReplyFeedback([FromBody] FeedbackDTO feedbackDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Invalid feedback data.");
                }

                if (string.IsNullOrWhiteSpace(feedbackDTO.Email))
                {
                    return BadRequest("Email address is required.");
                }

                // Send email to user
                var userEmail = feedbackDTO.Email;
                var emailSubject = $"Reply to your feedback: {feedbackDTO.Subject}";
                var emailBody = $"Dear User,\n\nThank you for your feedback.\n\nMessage:\n{feedbackDTO.Message}";

                await _emailService.SendEmailAsync(userEmail, emailSubject, emailBody);

                return Ok("Feedback replied successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error replying to feedback: {ex.Message}");
                return StatusCode(500, "An error occurred while replying to feedback.");
            }
        }

        [HttpDelete("DeleteFeedback")]
        public IActionResult DeleteFeedback([FromQuery] string feedbackId)
        {
            try
            {
                if (feedbackId == null)
                {
                    return BadRequest("Feedback ID is required.");
                }

                var feedbackExists = _feedbackServices.FeedbackExists(feedbackId);
                if (!feedbackExists)
                {
                    return NotFound("Feedback not found.");
                }

                _feedbackServices.DeleteFeedback(feedbackId);
                return Ok("Feedback deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting feedback: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting feedback.");
            }
        }
    }
}