using Microsoft.AspNetCore.Identity;

namespace EBookNepal.Entities
{
    public class User : IdentityUser
    {
        public required string Name { get; set; }
        public required string Address { get; set; }
        public required string ContactNo { get; set; }

        public string? ProfileImageUrl { get; set; }

        public bool IsSeller { get; set; } = false;

        public virtual ICollection<Book> Books { get; set; } = new List<Book>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}