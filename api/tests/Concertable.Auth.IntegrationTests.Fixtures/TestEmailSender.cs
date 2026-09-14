using Concertable.Shared.Email.Application;

namespace Concertable.Auth.IntegrationTests.Fixtures;

public sealed record SentEmail(string To, string Subject, string Body, string? Token = null);

public sealed class TestEmailSender : IEmailSender
{
    private readonly List<SentEmail> sent = new();

    public IReadOnlyList<SentEmail> Sent => sent;
    public Exception? Failure { get; set; }

    public Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        IReadOnlyList<EmailAttachment>? attachments = null)
    {
        if (Failure is not null)
            return Task.FromException(Failure);

        sent.Add(new SentEmail(toEmail, subject, body));
        return Task.CompletedTask;
    }

    public Task SendVerificationAsync(
        string toEmail,
        string token,
        string verifyBaseUrl,
        CancellationToken ct = default)
    {
        if (Failure is not null)
            return Task.FromException(Failure);

        sent.Add(new SentEmail(toEmail, "Verify your email", verifyBaseUrl, token));
        return Task.CompletedTask;
    }

    public void Reset()
    {
        sent.Clear();
        Failure = null;
    }
}
