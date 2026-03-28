namespace WalletService.Application.Interfaces;

public interface IEmailService
{
    Task SendTransferConfirmationAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
