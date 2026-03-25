using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
public class EmailSender : IEmailSender
{

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential("yt552448@gmail.com", "ccey afsb scwz dfbc")
        };

        return client.SendMailAsync(
        new MailMessage(from: "yt552448@gmail.com",
                        to: email,
                        subject,
                        htmlMessage
                        )
        {
            IsBodyHtml = true
        });
    }
}


