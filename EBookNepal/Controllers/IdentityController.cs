using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EBookNepal.Entities;
using EBookNepal.DTOS;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using EBookNepal.Services.Interfaces;

namespace EBookNepal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IdentityApiController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IdentityApiController> _logger;
        private readonly IEmailServices _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IdentityApiController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ILogger<IdentityApiController> logger,
            IEmailServices emailService,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] RegisterDTO registerDTO)
        {
            _logger.LogInformation("Starting user registration process.");

            // Validate the model state
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid.");
                return BadRequest(ModelState);
            }

            // Create a new user object
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = registerDTO.Email,
                Email = registerDTO.Email,
                Name = registerDTO.FullName,
                Address = registerDTO.Address,
                ContactNo = registerDTO.ContactNo,
                EmailConfirmed = false
            };

            // Handle profile image if provided (Cloudinary integration)
            if (registerDTO.ProfileImage != null && registerDTO.ProfileImage.Length > 0)
            {
                // Get the image service from DI
                var imageService = HttpContext.RequestServices.GetService(typeof(EBookNepal.Services.ImageServices)) as EBookNepal.Services.ImageServices;
                var uploadResult = await imageService.UploadPhotoAsync(registerDTO.ProfileImage);
                var imageUrl = uploadResult.SecureUrl?.ToString();

                user.ProfileImageUrl = imageUrl;
            }

            // Create the user in the database
            var result = await _userManager.CreateAsync(user, registerDTO.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    _logger.LogWarning($"Error creating user: {error.Description}");
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return BadRequest(ModelState);
            }

            // Assign the default "User" role
            if (!await _roleManager.RoleExistsAsync("User"))
            {
                await _roleManager.CreateAsync(new IdentityRole("User"));
            }
            await _userManager.AddToRoleAsync(user, "User");

            // Generate email confirmation token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action("ConfirmEmail", "Identity", new { userId = user.Id, token = token }, Request.Scheme);

            try
            {
                // Send confirmation email
                await _emailService.SendEmailAsync(user.Email, "Confirm your email", $"Please confirm your email by clicking <a href='{confirmationLink}'>here</a>.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending confirmation email: {ex.Message}");
                return StatusCode(500, "An error occurred while sending the confirmation email.");
            }

            _logger.LogInformation("User created successfully.");
            return Ok(new { Message = "Registration successful! Please confirm your email." });
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Invalid email confirmation request. UserId or Token is null or empty.");
                return BadRequest("Invalid email confirmation request.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found. UserId: {UserId}", userId);
                return NotFound("User not found.");
            }

            _logger.LogInformation("Attempting to confirm email for UserId: {UserId} with Token: {Token}", userId, token);

            try
            {
                // Decode the token
                var decodedToken = Uri.UnescapeDataString(token);

                // Confirm the email
                var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Email confirmed successfully for UserId: {UserId}", userId);
                    return Ok("Email confirmed successfully.");
                }

                _logger.LogWarning("Email confirmation failed for UserId: {UserId}. Errors: {Errors}", userId, result.Errors);
                return BadRequest("Email confirmation failed.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error confirming email for UserId: {userId}. Exception: {ex.Message}");
                return StatusCode(500, "An error occurred while confirming the email.");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            _logger.LogInformation("Starting user login process.");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid.");
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(loginDTO.Email);

            if (user != null)
            {
                // Check if the user's email is confirmed
                if (!user.EmailConfirmed)
                {
                    _logger.LogWarning("Login attempt failed. Email not confirmed for UserId: {UserId}", user.Id);
                    return BadRequest(new { Message = "Please verify your email to log in." });
                }

                var userRoles = await _userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Name!),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                authClaims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    expires: DateTime.Now.AddMinutes(double.Parse(_configuration["Jwt:ExpiryMinutes"]!)),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)),
                        SecurityAlgorithms.HmacSha256));

                _logger.LogInformation("User logged in successfully.");
                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    userId = user.Id,
                    roles = userRoles
                });
            }

            _logger.LogWarning("Invalid login attempt.");
            return Unauthorized();
        }

        [HttpGet("test-email")]
        public async Task<IActionResult> TestEmail()
        {
            try
            {
                await _emailService.SendEmailAsync("raajajmat252@gmail.com", "Test Email", "This is a test email.");
                return Ok("Email sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending test email: {ex.Message}");
                return StatusCode(500, "An error occurred while sending the test email.");
            }
        }
    }
}