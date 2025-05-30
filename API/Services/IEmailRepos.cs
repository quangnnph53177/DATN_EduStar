namespace API.Services
{
    public interface IEmailRepos
    {
        Task SendEmail(string toEmail, string subject, string htmlMessage);
    }
}
