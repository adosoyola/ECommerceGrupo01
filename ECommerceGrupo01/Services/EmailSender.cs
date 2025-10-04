using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace ECommerce.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Aquí no enviamos nada, solo simulamos
            return Task.CompletedTask;
        }
    }
}
