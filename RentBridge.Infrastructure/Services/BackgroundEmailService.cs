using RentBridge.Application.Common.Interfaces;

namespace RentBridge.Infrastructure.Services;

/// <summary>
/// Emails through the generic background dispatcher so the request pipeline
/// never blocks on external mailing latency, and a Brevo outage can no longer
/// fail a committed request (the scheduler retries failed sends with backoff).
/// </summary>
public class BackgroundEmailService(
    IBackgroundJobDispatcher dispatcher,
    IEmailSender emailSender) : IEmailService
{
    public Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        dispatcher.Enqueue<IEmailSender>(sender => sender.SendEmailAsync(to, subject, body, CancellationToken.None));
        return Task.CompletedTask;
    }
}