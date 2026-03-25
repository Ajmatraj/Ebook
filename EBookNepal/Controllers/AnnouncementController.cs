namespace EBookNepal.Controllers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using EBookNepal.Data;
    using EBookNepal.DTOS;
    using EBookNepal.Entities;
    using EBookNepal.Services.Interfaces;
    using Microsoft.AspNetCore.Mvc;

    [Route("api/[controller]")]
    [ApiController]
    public class AnnouncementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailServices _emailService;
        private readonly ILogger<AnnouncementController> _logger;
        private readonly IFeedbackServices _feedbackServices;
        public AnnouncementController(ApplicationDbContext context, IEmailServices emailService, ILogger<AnnouncementController> logger, IFeedbackServices feedbackServices)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _feedbackServices = feedbackServices;
        }

        [HttpPost("CreateBannerAnnouncement")]
        public async Task<IActionResult> CreateBannerAnnouncement([FromBody] BannerAnnouncementDTO bannerAnnouncementDTO)
        {
            try
            {
                if (bannerAnnouncementDTO == null || string.IsNullOrEmpty(bannerAnnouncementDTO.Title) || string.IsNullOrEmpty(bannerAnnouncementDTO.Message))
                {
                    return BadRequest("Title and message are required.");
                }

                var bannerAnnouncement = new BannerAnnouncement
                {
                    Title = bannerAnnouncementDTO.Title,
                    Message = bannerAnnouncementDTO.Message,
                    StartTime = bannerAnnouncementDTO.StartTime,
                    EndTime = bannerAnnouncementDTO.EndTime,
                    IsActive = true
                };

                _context.BannerAnnouncements.Add(bannerAnnouncement);
                await _context.SaveChangesAsync();

                // Send email to all users
                var userEmails = _context.Users.Select(u => u.Email).ToList();
                foreach (var email in userEmails)
                {
                    await _emailService.SendEmailAsync(email, bannerAnnouncement.Title, bannerAnnouncement.Message);
                }

                return Ok("Banner announcement created and emails sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating banner announcement: {ex.Message}");
                return StatusCode(500, "An error occurred while creating the banner announcement.");
            }
        }

        [HttpGet("GetActiveBanners")]
        public IActionResult GetActiveBanners()
        {
            try
            {
                var currentTime = DateTime.UtcNow;
                var activeBanners = _context.BannerAnnouncements
                    .Where(b => b.IsActive && b.StartTime <= currentTime && b.EndTime >= currentTime)
                    .ToList();

                return Ok(activeBanners);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving active banners: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving active banners.");
            }
        }

        [HttpPost("SendEmailToAllUsers")]
        public async Task<IActionResult> SendEmailToAllUsers([FromBody] EmailDTO emailDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Invalid email data.");
                }

                // Retrieve all user emails from the service
                var userEmails = _feedbackServices.GetAllUserEmails();

                if (userEmails == null || !userEmails.Any())
                {
                    return NotFound("No users found to send emails.");
                }

                // Send email to each user
                foreach (var email in userEmails)
                {
                    await _emailService.SendEmailAsync(email, emailDTO.Subject, emailDTO.Body);
                }

                return Ok("Emails sent successfully to all users.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending emails to all users: {ex.Message}");
                return StatusCode(500, "An error occurred while sending emails to all users.");
            }
        }
    }
}