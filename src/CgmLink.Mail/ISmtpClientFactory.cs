using MailKit.Net.Smtp;

namespace CgmLink.Mail;

internal interface ISmtpClientFactory
{
    ISmtpClient Create();
}