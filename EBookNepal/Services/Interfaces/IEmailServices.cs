namespace EBookNepal.Services.Interfaces
{
    public interface IEmailServices
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}