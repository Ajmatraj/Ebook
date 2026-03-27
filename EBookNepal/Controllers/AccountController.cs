using CloudinaryDotNet.Actions;
using EBookNepal.DTOS;
using EBookNepal.Entities;
using EBookNepal.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EBookNepal.Controllers
{
    [ApiController]
    [Route("")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly IImageServices _imageService;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ILogger<AccountController> logger,
            IImageServices imageService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _imageService = imageService;
        }

        // ============================
        // UPDATE PROFILE
        // ============================

        [Authorize]
        [HttpPut("update-profile/{userId}")]
        public async Task<IActionResult> UpdateProfile(
            string userId,
            [FromForm] UpdateProfileDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var callerRole = User.FindFirstValue(ClaimTypes.Role);

            if (callerId != userId && callerRole != "Admin")
                return Forbid();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            user.Name = dto.Name;
            user.Address = dto.Address;
            user.ContactNo = dto.ContactNo;
            user.UpdatedDate = DateTime.UtcNow;
            user.UpdatedBy = callerId;

            if (dto.ProfileImage != null && dto.ProfileImage.Length > 0)
            {
                if (!string.IsNullOrEmpty(user.ProfileImageUrl))
                {
                    var oldId = ExtractCloudinaryPublicId(user.ProfileImageUrl);
                    if (!string.IsNullOrEmpty(oldId))
                        await _imageService.DeletePhotoAsync(oldId);
                }

                var upload = await _imageService.UploadPhotoAsync(dto.ProfileImage);
                user.ProfileImageUrl = upload.SecureUrl?.ToString();
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                Message = "Profile updated successfully.",
                user.Id,
                user.Name,
                user.Email,
                user.Address,
                user.ContactNo,
                user.ProfileImageUrl,
                user.EmailConfirmed,
                Roles = roles
            });
        }

        // ============================
        // GET USER BY ID
        // ============================

        [Authorize]
        [HttpGet("get-user/{userId}")]
        public async Task<IActionResult> GetUserById(string userId)
        {
            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var callerRole = User.FindFirstValue(ClaimTypes.Role);

            if (callerId != userId && callerRole != "Admin")
                return Forbid();

            var user = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
                return NotFound(new { Message = "User not found." });

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Address,
                user.ContactNo,
                user.ProfileImageUrl,
                user.CreatedDate,
                user.EmailConfirmed,
                user.IsDeleted,
                Roles = roles
            });
        }

        // ============================
        // GET ALL USERS
        // ============================

        [Authorize(Roles = "Admin")]
        [HttpGet("get-all-users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userManager.Users
                .AsNoTracking()
                .Where(u => !u.IsDeleted)
                .ToListAsync();

            if (!users.Any())
                return NotFound(new { Message = "No users found." });

            var list = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                list.Add(new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Address,
                    user.ContactNo,
                    user.ProfileImageUrl,
                    user.CreatedDate,
                    user.EmailConfirmed,
                    Roles = roles
                });
            }

            return Ok(list);
        }

        // ============================
        // DELETE USER (SOFT)
        // ============================

        [Authorize]
        [HttpDelete("delete-user/{userId}")]
        public async Task<IActionResult> DeleteUserById(string userId)
        {
            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var callerRole = User.FindFirstValue(ClaimTypes.Role);

            if (callerId != userId && callerRole != "Admin")
                return Forbid();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            user.IsDeleted = true;
            user.UpdatedDate = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { Message = "User deleted successfully." });
        }

        // ============================
        // CHANGE PASSWORD
        // ============================

        [Authorize]
        [HttpPut("change-password/{userId}")]
        public async Task<IActionResult> ChangePassword(
            string userId,
            [FromBody] ChangePasswordDTO dto)
        {
            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (callerId != userId)
                return Forbid();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            var result = await _userManager.ChangePasswordAsync(
                user,
                dto.CurrentPassword,
                dto.NewPassword);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { Message = "Password changed successfully." });
        }

        // ============================
        // CLOUDINARY HELPER
        // ============================

        private static string? ExtractCloudinaryPublicId(string imageUrl)
        {
            try
            {
                var uri = new Uri(imageUrl);
                var path = uri.AbsolutePath;
                const string marker = "/upload/";
                var idx = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) return null;

                var afterUpload = path[(idx + marker.Length)..];
                var segments = afterUpload.Split('/');

                var start = segments[0].StartsWith('v') &&
                            segments[0].Length > 1 &&
                            segments[0][1..].All(char.IsDigit) ? 1 : 0;

                var publicIdWithExt = string.Join("/", segments[start..]);
                var dot = publicIdWithExt.LastIndexOf('.');
                return dot >= 0 ? publicIdWithExt[..dot] : publicIdWithExt;
            }
            catch
            {
                return null;
            }
        }
    }
}