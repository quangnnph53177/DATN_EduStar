
using Azure.Core;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Net;
using System.Net.Mail;

namespace API.Services.Repositories
{
    public class EmailRepos : IEmailRepos
    {
        private readonly IConfiguration _config;
        public EmailRepos(IConfiguration configuration)
        {
            _config = configuration;
        }
        public async Task SendEmail(string toEmail, string subject, string htmlMessage)
        {
            var smtpClient = new SmtpClient(_config["Smtp:Host"])
            {
                Port = int.Parse(_config["Smtp:Port"]),
                Credentials = new NetworkCredential(_config["Smtp:User"], _config["Smtp:Password"]),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_config["Smtp:Sender"], "Cơ Sở Đào tạo EduStar"),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);
            await smtpClient.SendMailAsync(mailMessage);

        }
    }
}