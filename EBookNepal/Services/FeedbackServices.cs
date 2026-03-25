namespace EBookNepal.Services
{
    using EBookNepal.Data;
    using EBookNepal.DTOS;
    using EBookNepal.Entities;
    using EBookNepal.Services.Interfaces;

    public class FeedbackServices : IFeedbackServices
    {
        private readonly ApplicationDbContext _context;

        public FeedbackServices(ApplicationDbContext context)
        {
            _context = context;
        }

        public void SendFeedback(FeedbackDTO feedbackDto)
        {
            var feedback = new Feedback
            {
                UserId = feedbackDto.UserId,
                Email = feedbackDto.Email,
                Subject = feedbackDto.Subject,
                Message = feedbackDto.Message
            };

            _context.Feedbacks.Add(feedback);
            _context.SaveChanges();
        }

        public IEnumerable<FeedbackDTO> GetFeedbacks()
        {
            var feedbacks = _context.Feedbacks.ToList();
            return feedbacks.Select(f => new FeedbackDTO
            {
                UserId = f.UserId,
                Email = f.Email,
                Subject = f.Subject,
                Message = f.Message,
            });
        }

        public List<string> GetAllUserEmails()
        {
            return _context.Users.Select(u => u.Email).ToList();
        }

        public bool FeedbackExists(string feedbackId)
        {
            return _context.Feedbacks.Any(f => f.FeedbackId == feedbackId.ToString());
        }

        public void DeleteFeedback(string feedbackId)
        {
            var feedback = _context.Feedbacks.FirstOrDefault(f => f.FeedbackId == feedbackId.ToString());
            if (feedback != null)
            {
                _context.Feedbacks.Remove(feedback);
                _context.SaveChanges();
            }
        }
    }
}