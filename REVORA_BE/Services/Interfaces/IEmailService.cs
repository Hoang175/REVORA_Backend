using System.Threading.Tasks;

namespace REVORA_BE.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }
}
