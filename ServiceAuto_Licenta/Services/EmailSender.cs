using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace ServiceAutoLicenta.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration config;

        public EmailSender(IConfiguration config)
        {
            this.config=config;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            string? host=config["Email:SmtpHost"];
            int port=config.GetValue<int>("Email:SmtpPort",587);
            string? user=config["Email:SmtpUser"];
            string? pass=config["Email:SmtpPass"];
            string? from=config["Email:From"];
            string? fromName=config["Email:FromName"];
            if(from==null) from="noreply@garagecare.ro";
            var client=new SmtpClient(host)
            {
                Port=port,
                Credentials=new NetworkCredential(user,pass),
                EnableSsl=true
            };
            var mail=new MailMessage
            {
                From=new MailAddress(from,fromName),
                Subject=subject,
                Body=htmlMessage,
                IsBodyHtml=true
            };
            mail.To.Add(email);
            await client.SendMailAsync(mail);
        }
    }
}
