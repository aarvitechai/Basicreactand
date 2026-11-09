using System.Text.Encodings.Web;
using RedStarVibe.Application.Abstractions;

namespace RedStarVibe.Infrastructure.Email;

internal static class EmailTemplates
{
    public static EmailMessage PasswordReset(string to, string displayName, Uri resetLink, string appName, TimeSpan validFor)
    {
        var html = HtmlEncoder.Default;
        var name = html.Encode(displayName);
        var link = html.Encode(resetLink.AbsoluteUri);
        var app = html.Encode(appName);
        var minutes = (int)validFor.TotalMinutes;

        var htmlBody = $"""
            <!doctype html>
            <html>
              <body style="margin:0;padding:24px;background:#f7f7f8;font-family:Segoe UI,Roboto,Arial,sans-serif;color:#1c1c21">
                <table role="presentation" width="100%" style="max-width:520px;margin:0 auto;background:#ffffff;border-radius:10px;padding:32px">
                  <tr><td>
                    <h1 style="margin:0 0 16px;font-size:22px;color:#c62828">{app}</h1>
                    <p>Hi {name},</p>
                    <p>We received a request to reset the password for your account. Click the button below to choose a new one.</p>
                    <p style="margin:28px 0">
                      <a href="{link}" style="background:#c62828;color:#ffffff;text-decoration:none;padding:12px 22px;border-radius:8px;font-weight:600;display:inline-block">Reset password</a>
                    </p>
                    <p style="font-size:14px;color:#6b6b76">This link expires in {minutes} minutes and can only be used once.</p>
                    <p style="font-size:14px;color:#6b6b76">If you didn't request this, you can safely ignore this email. Your password won't change.</p>
                    <p style="font-size:12px;color:#9a9aa6;word-break:break-all">If the button doesn't work, paste this link into your browser:<br>{link}</p>
                  </td></tr>
                </table>
              </body>
            </html>
            """;

        var textBody = $"""
            Hi {displayName},

            We received a request to reset the password for your {appName} account.
            Open this link to choose a new password (expires in {minutes} minutes):

            {resetLink.AbsoluteUri}

            If you didn't request this, you can ignore this email. Your password won't change.
            """;

        return new EmailMessage(to, $"Reset your {appName} password", htmlBody, textBody);
    }
}
