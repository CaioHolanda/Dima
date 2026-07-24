using Dima.Api.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Dima.Api.Services.Email;

public class EmailSender : IEmailSender<User>
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(
        IOptions<EmailOptions> options,
        ILogger<EmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SendConfirmationLinkAsync(
        User user,
        string email,
        string confirmationLink)
    {
        const string subject = "Confirme o e-mail cadastrado";

        var originalUri = new Uri(confirmationLink);
        var query = originalUri.Query;

        var frontendConfirmationUrl =
            $"{_options.FrontendBaseUrl.TrimEnd('/')}/confirm-email{query}";

        var recipientName = user.Email ?? email;

        var body = EmailTemplates.Confirmation(
            recipientName,
            frontendConfirmationUrl);

        return SendEmailAsync(
            email,
            subject,
            body);
    }

    public Task SendPasswordResetCodeAsync(
        User user,
        string email,
        string resetCode)
    {
        const string subject = "Código para redefinição de senha";

        var recipientName = user.Email ?? email;

        var resetUrl =
            $"{_options.FrontendBaseUrl.TrimEnd('/')}/reset-password" +
            $"?email={Uri.EscapeDataString(email)}" +
            $"&code={Uri.EscapeDataString(resetCode)}";

        var body = EmailTemplates.PasswordResetLink(
            recipientName,
            resetUrl);

        return SendEmailAsync(
            email,
            subject,
            body);
    }

    public Task SendPasswordResetLinkAsync(
        User user,
        string email,
        string resetLink)
    {
        const string subject = "Redefina sua senha no Dima";

        var recipientName = user.Email ?? email;

        var body = EmailTemplates.PasswordResetLink(
            recipientName,
            resetLink);

        return SendEmailAsync(
            email,
            subject,
            body);
    }

    private async Task SendEmailAsync(
        string recipient,
        string subject,
        string htmlBody)
    {
        ValidateOptions();

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                _options.FromName,
                _options.FromAddress));

        message.To.Add(
            MailboxAddress.Parse(recipient));

        message.Subject = subject;

        message.Body = new BodyBuilder
        {
            HtmlBody = htmlBody
        }.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                _options.UserName,
                _options.Password);

            await client.SendAsync(message);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Erro ao enviar e-mail para {Recipient}",
                recipient);

            throw;
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true);
            }
        }
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            throw new InvalidOperationException(
                "A configuração Email:Host não foi informada.");
        }

        if (_options.Port <= 0)
        {
            throw new InvalidOperationException(
                "A configuração Email:Port é inválida.");
        }

        if (string.IsNullOrWhiteSpace(_options.UserName))
        {
            throw new InvalidOperationException(
                "A configuração Email:UserName não foi informada.");
        }

        if (string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new InvalidOperationException(
                "A configuração Email:Password não foi informada.");
        }

        if (string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            throw new InvalidOperationException(
                "A configuração Email:FromAddress não foi informada.");
        }
        if (string.IsNullOrWhiteSpace(_options.FrontendBaseUrl))
        {
            throw new InvalidOperationException(
                "A configuração Email:FrontendBaseUrl não foi informada.");
        }
    }
}