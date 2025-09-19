using StartEvent_API.Models.Email;

namespace StartEvent_API.Services.Email
{
    /// <summary>
    /// Interface for email service operations
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends a single email using a template
        /// </summary>
        /// <param name="template">Email template with data</param>
        /// <returns>Email send result</returns>
        Task<EmailSendResult> SendEmailAsync<T>(T template) where T : EmailTemplateBase;

        /// <summary>
        /// Sends multiple emails in bulk
        /// </summary>
        /// <param name="templates">List of email templates</param>
        /// <returns>Bulk email send result</returns>
        Task<BulkEmailSendResult> SendBulkEmailsAsync<T>(List<T> templates) where T : EmailTemplateBase;

        /// <summary>
        /// Sends a welcome email to a new user
        /// </summary>
        /// <param name="template">Welcome email template</param>
        /// <returns>Email send result</returns>
        Task<EmailSendResult> SendWelcomeEmailAsync(WelcomeEmailTemplate template);

        /// <summary>
        /// Sends a ticket confirmation email
        /// </summary>
        /// <param name="template">Ticket confirmation template</param>
        /// <returns>Email send result</returns>
        Task<EmailSendResult> SendTicketConfirmationEmailAsync(TicketConfirmationEmailTemplate template);

        /// <summary>
        /// Sends a payment confirmation email
        /// </summary>
        /// <param name="template">Payment confirmation template</param>
        /// <returns>Email send result</returns>
        Task<EmailSendResult> SendPaymentConfirmationEmailAsync(PaymentConfirmationEmailTemplate template);

        /// <summary>
        /// Sends an event reminder email
        /// </summary>
        /// <param name="template">Event reminder template</param>
        /// <returns>Email send result</returns>
        Task<EmailSendResult> SendEventReminderEmailAsync(EventReminderEmailTemplate template);

        /// <summary>
        /// Sends an event cancellation email
        /// </summary>
        /// <param name="template">Event cancellation template</param>
        /// <returns>Email send result</returns>
        Task<EmailSendResult> SendEventCancellationEmailAsync(EventCancellationEmailTemplate template);

        /// <summary>
        /// Sends a password reset email
        /// </summary>
        /// <param name="template">Password reset template</param>
        /// <returns>Email send result</returns>
        Task<EmailSendResult> SendPasswordResetEmailAsync(PasswordResetEmailTemplate template);

        /// <summary>
        /// Sends an email verification email
        /// </summary>
        /// <param name="template">Email verification template</param>
        /// <returns>Email send result</returns>
        Task<EmailSendResult> SendEmailVerificationEmailAsync(EmailVerificationEmailTemplate template);

        /// <summary>
        /// Validates email configuration and connectivity
        /// </summary>
        /// <returns>True if email service is properly configured and accessible</returns>
        Task<bool> ValidateEmailServiceAsync();

        /// <summary>
        /// Gets email service health status
        /// </summary>
        /// <returns>Service health information</returns>
        Task<EmailServiceHealth> GetServiceHealthAsync();
    }

    /// <summary>
    /// Email service health status
    /// </summary>
    public class EmailServiceHealth
    {
        /// <summary>
        /// Whether the service is healthy
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Service status message
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Last successful email sent timestamp
        /// </summary>
        public DateTime? LastSuccessfulEmail { get; set; }

        /// <summary>
        /// Error details if unhealthy
        /// </summary>
        public string? ErrorDetails { get; set; }

        /// <summary>
        /// Response time in milliseconds
        /// </summary>
        public double ResponseTimeMs { get; set; }
    }
}