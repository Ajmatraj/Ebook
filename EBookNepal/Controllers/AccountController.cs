using System.Security.Claims;
using EBookNepal.DTOS;
using EBookNepal.Entities;
using EBookNepal.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            [FromForm] UpdateProfileDTO updateProfileDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrEmpty(userId))
                return BadRequest(new { Message = "User ID is required." });

            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var callerRole = User.FindFirstValue(ClaimTypes.Role);
            if (callerId != userId && callerRole != "Admin")
                return Forbid();

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { Message = "User not found." });

            user.Name = updateProfileDTO.Name;
            user.Address = updateProfileDTO.Address;
            user.ContactNo = updateProfileDTO.ContactNo;
            user.UpdatedDate = DateTime.UtcNow;
            user.UpdatedBy = callerId;

            if (updateProfileDTO.ProfileImage != null &&
                updateProfileDTO.ProfileImage.Length > 0)
            {
                // Delete old Cloudinary asset before uploading replacement
                if (!string.IsNullOrEmpty(user.ProfileImageUrl))
                {
                    var oldPublicId = ExtractCloudinaryPublicId(user.ProfileImageUrl);
                    if (!string.IsNullOrEmpty(oldPublicId))
                        await _imageService.DeletePhotoAsync(oldPublicId);
                }

                var uploadResult = await _imageService.UploadPhotoAsync(
                    updateProfileDTO.ProfileImage);

                user.ProfileImageUrl = uploadResult.SecureUrl?.ToString();
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return BadRequest(ModelState);
            }

            _logger.LogInformation("User {UserId} profile updated.", userId);

            return Ok(new
            {
                Message = "Profile updated successfully.",
                user.ProfileImageUrl
            });
        }

        // ============================
        // GET USER BY ID
        // ============================

        [Authorize]
        [HttpGet("get-user/{userId}")]
        public async Task<IActionResult> GetUserById(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest(new { Message = "User ID is required." });

            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var callerRole = User.FindFirstValue(ClaimTypes.Role);
            if (callerId != userId && callerRole != "Admin")
                return Forbid();

            var user = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { Message = "User not found." });

            return Ok(new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Address,
                user.ContactNo,
                user.ProfileImageUrl,  // always this field
                user.CreatedDate
            });
        }

        // ============================
        // GET ALL USERS (Admin only)
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

            var userList = users.Select(user => new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Address,
                user.ContactNo,
                user.ProfileImageUrl,
                user.CreatedDate
            });

            return Ok(userList);
        }

        // ============================
        // DELETE USER (soft delete)
        // ============================

        [Authorize]
        [HttpDelete("delete-user/{userId}")]
        public async Task<IActionResult> DeleteUserById(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest(new { Message = "User ID is required." });

            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var callerRole = User.FindFirstValue(ClaimTypes.Role);
            if (callerId != userId && callerRole != "Admin")
                return Forbid();

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { Message = "User not found." });

            user.IsDeleted = true;
            user.UpdatedDate = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return BadRequest(ModelState);
            }

            _logger.LogInformation("User {UserId} soft-deleted.", userId);
            return Ok(new { Message = "User deleted successfully." });
        }

        // ============================
        // CHANGE PASSWORD
        // ============================

        [Authorize]
        [HttpPut("change-password/{userId}")]
        public async Task<IActionResult> ChangePassword(
            string userId,
            [FromBody] ChangePasswordDTO changePasswordDTO)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest(new { Message = "User ID is required." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (callerId != userId)
                return Forbid();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            var result = await _userManager.ChangePasswordAsync(
                user,
                changePasswordDTO.CurrentPassword,
                changePasswordDTO.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return BadRequest(ModelState);
            }

            _logger.LogInformation("Password changed for user {UserId}.", userId);
            return Ok(new { Message = "Password changed successfully." });
        }

        // ============================
        // HELPERS
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