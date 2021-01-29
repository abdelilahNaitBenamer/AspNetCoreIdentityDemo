using Microsoft.Extensions.Configuration;
using static ElasticEmailClient.Api;
using System;
using System.Threading.Tasks;

namespace UsersManagement.Services{
    public class EmailSender : IEmailSender
    {
        public IConfiguration Configuration { get; }

        public EmailSender(IConfiguration config)
        {
            Configuration = config;
        }
        
        /// Send email
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            ApiKey = Configuration["ElasticMailAPIKey"];
            await SendEmail(subject, Configuration["FromEmail"], "Abdelilah", new string[] { email },
                                htmlMessage, htmlMessage);
        }

        public async static Task<ElasticEmailClient.ApiTypes.EmailSend> SendEmail(string subject, string fromEmail, string fromName, string[] msgTo, string html, string text)
        {
            try
            {
                return await ElasticEmailClient.Api.Email.SendAsync(subject, fromEmail, fromName, msgTo: msgTo, bodyHtml: html, bodyText: text);
            }
            catch (Exception ex)
            {
                if (ex is ApplicationException)
                    Console.WriteLine("Server didn't accept the request: " + ex.Message);
                else
                    Console.WriteLine("Something unexpected happened: " + ex.Message);

                return null;
            }
        }
    }
}