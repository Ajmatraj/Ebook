namespace EBookNepal.Entities
{
    public class Feedback
    {
        public string FeedbackId { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; }
        public string Email { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public virtual User User { get; set; }
    }
}