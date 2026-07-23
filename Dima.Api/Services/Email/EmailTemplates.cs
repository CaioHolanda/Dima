using System.Net;

namespace Dima.Api.Services.Email;

public static class EmailTemplates
{
    public static string Confirmation(
        string recipientName,
        string confirmationLink)
    {
        var safeRecipientName = WebUtility.HtmlEncode(recipientName);

        return $"""
            <h2>Confirmação de e-mail</h2>

            <p>Olá, {safeRecipientName}.</p>

            <p>
                Clique no link abaixo para confirmar seu endereço de e-mail.
            </p>

            <p>
                <a href="{confirmationLink}">
                    Confirmar e-mail
                </a>
            </p>

            <p>
                Caso você não tenha solicitado este cadastro,
                ignore esta mensagem.
            </p>
            """;
    }

    public static string PasswordResetLink(
        string recipientName,
        string resetLink)
    {
        var safeRecipientName = WebUtility.HtmlEncode(recipientName);

        return $"""
            <h2>Redefinição de senha</h2>

            <p>Olá, {safeRecipientName}.</p>

            <p>
                Clique no link abaixo para criar uma nova senha.
            </p>

            <p>
                <a href="{resetLink}">
                    Redefinir senha
                </a>
            </p>

            <p>
                Caso você não tenha solicitado a redefinição,
                ignore esta mensagem.
            </p>
            """;
    }

    public static string PasswordResetCode(
        string recipientName,
        string resetCode)
    {
        var safeRecipientName = WebUtility.HtmlEncode(recipientName);
        var safeResetCode = WebUtility.HtmlEncode(resetCode);

        return $"""
            <h2>Redefinição de senha</h2>

            <p>Olá, {safeRecipientName}.</p>

            <p>Use o código abaixo para redefinir sua senha:</p>

            <p>
                <strong>{safeResetCode}</strong>
            </p>

            <p>
                Caso você não tenha solicitado a redefinição,
                ignore esta mensagem.
            </p>
            """;
    }
}