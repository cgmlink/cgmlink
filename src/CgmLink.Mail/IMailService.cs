namespace CgmLink.Mail;

public interface IMailService
{
    Task SendAsync(MailRequest request, CancellationToken cancellationToken = default);
}