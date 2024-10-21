using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace Clinic.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        public EmailService(IConfiguration configuration)
        {
            this._configuration= configuration;
        }
        public async Task SendEmailAsync(string recipientEmail, string token,string receiverName)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_configuration["EmailSettings:SenderName"], _configuration["EmailSettings:SenderEmail"]));
            message.To.Add(new MailboxAddress(receiverName, recipientEmail));
            message.Subject = "Testing 123";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                <form action='{_configuration["APPURL:URL"]}/api/Auth/verify-token' method='get' target='_blank'>
                   <input type='hidden' name='token' value='{token}' />
                   <input type='submit' value='Submit' />
                </form>
                <p>
                    Click on the submit button, and the form will be submitted using the GET method.
                 </p>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                client.Connect(_configuration["EmailSettings:SmtpServer"], int.Parse(_configuration["EmailSettings:SmtpPort"]), false);
                client.Authenticate(_configuration["EmailSettings:Username"], _configuration["EmailSettings:Password"]);

                await client.SendAsync(message);
                client.Disconnect(true);
            }
        }

        public async Task SEndOtp(string recipientEmail, string code, string receiverName)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_configuration["EmailSettings:SenderName"], _configuration["EmailSettings:SenderEmail"]));
            message.To.Add(new MailboxAddress(receiverName, recipientEmail));
            message.Subject = $"Hello {receiverName}";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $"your code is {code}"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                client.Connect(_configuration["EmailSettings:SmtpServer"], int.Parse(_configuration["EmailSettings:SmtpPort"]), false);
                client.Authenticate(_configuration["EmailSettings:Username"], _configuration["EmailSettings:Password"]);

                await client.SendAsync(message);
                client.Disconnect(true);
            }
        }
    }
}
