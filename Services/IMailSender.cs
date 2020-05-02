using System.Threading.Tasks;

namespace UsersManagement.Services{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
}