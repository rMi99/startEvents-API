using StartEvent_API.Models.Email;

namespace StartEvent_API.Services.Email
{
    /// <summary>
    /// Interface for email template rendering service
    /// </summary>
    public interface IEmailTemplateService
    {
        /// <summary>
        /// Renders an email template to HTML and text content
        /// </summary>
        /// <typeparam name="T">Template type</typeparam>
        /// <param name="template">Template data</param>
        /// <returns>Rendered template result</returns>
        Task<EmailTemplateResult> RenderTemplateAsync<T>(T template) where T : EmailTemplateBase;

        /// <summary>
        /// Renders a welcome email template
        /// </summary>
        /// <param name="template">Welcome email template</param>
        /// <returns>Rendered template result</returns>
        Task<EmailTemplateResult> RenderWelcomeEmailAsync(WelcomeEmailTemplate template);

        /// <summary>
        /// Renders a ticket confirmation email template
        /// </summary>
        /// <param name="template">Ticket confirmation template</param>
        /// <returns>Rendered template result</returns>
        Task<EmailTemplateResult> RenderTicketConfirmationEmailAsync(TicketConfirmationEmailTemplate template);

        /// <summary>
        /// Renders a payment confirmation email template
        /// </summary>
        /// <param name="template">Payment confirmation template</param>
        /// <returns>Rendered template result</returns>
        Task<EmailTemplateResult> RenderPaymentConfirmationEmailAsync(PaymentConfirmationEmailTemplate template);

        /// <summary>
        /// Renders an event reminder email template
        /// </summary>
        /// <param name="template">Event reminder template</param>
        /// <returns>Rendered template result</returns>
        Task<EmailTemplateResult> RenderEventReminderEmailAsync(EventReminderEmailTemplate template);

        /// <summary>
        /// Renders an event cancellation email template
        /// </summary>
        /// <param name="template">Event cancellation template</param>
        /// <returns>Rendered template result</returns>
        Task<EmailTemplateResult> RenderEventCancellationEmailAsync(EventCancellationEmailTemplate template);

        /// <summary>
        /// Renders a password reset email template
        /// </summary>
        /// <param name="template">Password reset template</param>
        /// <returns>Rendered template result</returns>
        Task<EmailTemplateResult> RenderPasswordResetEmailAsync(PasswordResetEmailTemplate template);

        /// <summary>
        /// Renders an email verification template
        /// </summary>
        /// <param name="template">Email verification template</param>
        /// <returns>Rendered template result</returns>
        Task<EmailTemplateResult> RenderEmailVerificationEmailAsync(EmailVerificationEmailTemplate template);

        /// <summary>
        /// Validates template data before rendering
        /// </summary>
        /// <typeparam name="T">Template type</typeparam>
        /// <param name="template">Template to validate</param>
        /// <returns>Validation result with errors if any</returns>
        Task<TemplateValidationResult> ValidateTemplateAsync<T>(T template) where T : EmailTemplateBase;
    }

    /// <summary>
    /// Template validation result
    /// </summary>
    public class TemplateValidationResult
    {
        /// <summary>
        /// Whether the template is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// List of validation errors
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// List of validation warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new();
    }
}