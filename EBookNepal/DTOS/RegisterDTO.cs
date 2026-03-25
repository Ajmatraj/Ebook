using System.ComponentModel.DataAnnotations;

namespace EBookNepal.DTOS
{
    public class RegisterDTO
    {
        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {3} and at max {100} characters long.", MinimumLength = 3)]
        public string Address { get; set; }

        [Required]
        [Phone]
        public string ContactNo { get; set; }

        public IFormFile? ProfileImage { get; set; }
    }
}