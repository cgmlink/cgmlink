using MailKit.Net.Smtp;

namespace CgmLink.Mail;

internal sealed class SmtpClientFactory : ISmtpClientFactory
{
    public ISmtpClient Create()
    {
        return new SmtpClient();
    }
}