namespace EBookNepal.Entities
{
    public class Image
    {
        public string ImageId { get; set; } = Guid.NewGuid().ToString();
        public required string FileName { get; set; }
        public required string FilePath { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
