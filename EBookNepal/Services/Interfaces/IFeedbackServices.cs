namespace EBookNepal.Services.Interfaces
{
    using EBookNepal.DTOS;
    using EBookNepal.Entities;

    public interface IFeedbackServices
    {
        void SendFeedback(FeedbackDTO feedbackDto);
        IEnumerable<FeedbackDTO> GetFeedbacks();
        List<string> GetAllUserEmails();
        bool FeedbackExists(string feedbackId);
        void DeleteFeedback(string feedbackId);
    }
}