using Newtonsoft.Json;

namespace StartEvent_API.Models.Email
{
    /// <summary>
    /// Brevo API send transactional email request model
    /// </summary>
    public class BrevoSendEmailRequest
    {
        /// <summary>
        /// Sender information
        /// </summary>
        [JsonProperty("sender")]
        public BrevoEmailAddress Sender { get; set; } = new();

        /// <summary>
        /// List of recipients
        /// </summary>
        [JsonProperty("to")]
        public List<BrevoEmailAddress> To { get; set; } = new();

        /// <summary>
        /// List of CC recipients (optional)
        /// </summary>
        [JsonProperty("cc")]
        public List<BrevoEmailAddress>? Cc { get; set; }

        /// <summary>
        /// List of BCC recipients (optional)
        /// </summary>
        [JsonProperty("bcc")]
        public List<BrevoEmailAddress>? Bcc { get; set; }

        /// <summary>
        /// Email subject
        /// </summary>
        [JsonProperty("subject")]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// HTML content of the email
        /// </summary>
        [JsonProperty("htmlContent")]
        public string HtmlContent { get; set; } = string.Empty;

        /// <summary>
        /// Text content of the email (fallback)
        /// </summary>
        [JsonProperty("textContent")]
        public string? TextContent { get; set; }

        /// <summary>
        /// Email attachments
        /// </summary>
        [JsonProperty("attachment")]
        public List<BrevoAttachment>? Attachment { get; set; }

        /// <summary>
        /// Template parameters (if using Brevo templates)
        /// </summary>
        [JsonProperty("params")]
        public Dictionary<string, object>? Params { get; set; }

        /// <summary>
        /// Email tags for tracking
        /// </summary>
        [JsonProperty("tags")]
        public List<string>? Tags { get; set; }

        /// <summary>
        /// Custom headers
        /// </summary>
        [JsonProperty("headers")]
        public Dictionary<string, string>? Headers { get; set; }

        /// <summary>
        /// Template ID (if using Brevo templates instead of HTML content)
        /// </summary>
        [JsonProperty("templateId")]
        public long? TemplateId { get; set; }
    }

    /// <summary>
    /// Brevo email address model
    /// </summary>
    public class BrevoEmailAddress
    {
        /// <summary>
        /// Email address
        /// </summary>
        [JsonProperty("email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Display name (optional)
        /// </summary>
        [JsonProperty("name")]
        public string? Name { get; set; }
    }

    /// <summary>
    /// Brevo attachment model
    /// </summary>
    public class BrevoAttachment
    {
        /// <summary>
        /// Base64 encoded file content
        /// </summary>
        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// File name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Brevo API response for send email
    /// </summary>
    public class BrevoSendEmailResponse
    {
        /// <summary>
        /// Message ID from Brevo
        /// </summary>
        [JsonProperty("messageId")]
        public string MessageId { get; set; } = string.Empty;

        /// <summary>
        /// Response message
        /// </summary>
        [JsonProperty("message")]
        public string? Message { get; set; }
    }

    /// <summary>
    /// Brevo API error response
    /// </summary>
    public class BrevoErrorResponse
    {
        /// <summary>
        /// Error code
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Error message
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Additional error details
        /// </summary>
        [JsonProperty("details")]
        public Dictionary<string, object>? Details { get; set; }
    }

    /// <summary>
    /// Email sending result
    /// </summary>
    public class EmailSendResult
    {
        /// <summary>
        /// Whether the email was sent successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Message ID from the email service provider
        /// </summary>
        public string? MessageId { get; set; }

        /// <summary>
        /// Error message if sending failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Exception details if any
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Number of retry attempts made
        /// </summary>
        public int RetryAttempts { get; set; } = 0;

        /// <summary>
        /// Timestamp when the email was sent
        /// </summary>
        public DateTime SentAt { get; set; }

        /// <summary>
        /// Email recipient address
        /// </summary>
        public string RecipientEmail { get; set; } = string.Empty;

        /// <summary>
        /// Email subject
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Email template type that was sent
        /// </summary>
        public EmailTemplateType TemplateType { get; set; }
    }

    /// <summary>
    /// Bulk email sending result
    /// </summary>
    public class BulkEmailSendResult
    {
        /// <summary>
        /// Total number of emails attempted
        /// </summary>
        public int TotalEmails { get; set; }

        /// <summary>
        /// Number of emails sent successfully
        /// </summary>
        public int SuccessfulEmails { get; set; }

        /// <summary>
        /// Number of failed emails
        /// </summary>
        public int FailedEmails { get; set; }

        /// <summary>
        /// Individual email results
        /// </summary>
        public List<EmailSendResult> Results { get; set; } = new();

        /// <summary>
        /// Overall success rate percentage
        /// </summary>
        public double SuccessRate => TotalEmails > 0 ? (double)SuccessfulEmails / TotalEmails * 100 : 0;
    }
}