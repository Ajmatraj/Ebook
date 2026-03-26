using EBookNepal.DTOS;
using EBookNepal.Entities;
using EBookNepal.Services;
using EBookNepal.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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
        private readonly ImageServices _imageService;

        public IdentityApiController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ILogger<IdentityApiController> logger,
            IEmailServices emailService,
            ImageServices imageService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
            _imageService = imageService;
        }

        // ================= REGISTER =================

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] RegisterDTO registerDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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

            // Upload profile image
            if (registerDTO.ProfileImage != null)
            {
                var upload = await _imageService.UploadPhotoAsync(registerDTO.ProfileImage);
                user.ProfileImageUrl = upload.SecureUrl?.ToString();
            }

            var result = await _userManager.CreateAsync(user, registerDTO.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Assign default role
            if (!await _roleManager.RoleExistsAsync("User"))
                await _roleManager.CreateAsync(new IdentityRole("User"));

            await _userManager.AddToRoleAsync(user, "User");

            // Email confirmation
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var confirmationLink = Url.Action(
                nameof(ConfirmEmail),
                "IdentityApi",
                new { userId = user.Id, token },
                Request.Scheme);

            await _emailService.SendEmailAsync(
                user.Email!,
                "Confirm your email",
                $"Click <a href='{confirmationLink}'>here</a> to confirm.");

            return Ok(new
            {
                message = "Registration successful. Please verify your email."
            });
        }

        // ================= CONFIRM EMAIL =================

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var decodedToken = Uri.UnescapeDataString(token);

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok("Email confirmed successfully.");
        }

        // ================= LOGIN =================

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            var user = await _userManager.FindByEmailAsync(loginDTO.Email);

            if (user == null)
                return Unauthorized("Invalid credentials");

            if (!user.EmailConfirmed)
                return BadRequest("Please verify your email first.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDTO.Password, false);

            if (!result.Succeeded)
                return Unauthorized("Invalid credentials");

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Sub, user.Name!)
            };

            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                expires: DateTime.UtcNow.AddMinutes(
                    double.Parse(_configuration["Jwt:ExpiryMinutes"]!)),
                claims: claims,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                userId = user.Id,
                roles
            });
        }

        // ================= EMAIL TEST =================

        [HttpGet("test-email")]
        public async Task<IActionResult> TestEmail()
        {
            await _emailService.SendEmailAsync(
                "test@example.com",
                "Test Email",
                "Email service working.");

            return Ok("Email sent");
        }
    }
}