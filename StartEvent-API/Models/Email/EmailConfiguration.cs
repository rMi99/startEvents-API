namespace StartEvent_API.Models.Email
{
    /// <summary>
    /// Configuration settings for email service integration
    /// </summary>
    public class EmailConfiguration
    {
        /// <summary>
        /// Brevo API configuration settings
        /// </summary>
        public BrevoSettings Brevo { get; set; } = new();

        /// <summary>
        /// General email settings
        /// </summary>
        public GeneralEmailSettings General { get; set; } = new();

        /// <summary>
        /// Template configuration settings
        /// </summary>
        public TemplateSettings Templates { get; set; } = new();
    }

    /// <summary>
    /// Brevo (Sendinblue) specific configuration
    /// </summary>
    public class BrevoSettings
    {
        /// <summary>
        /// Brevo API key for authentication
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Brevo API base URL (default: https://api.brevo.com/v3)
        /// </summary>
        public string ApiUrl { get; set; } = "https://api.brevo.com/v3";

        /// <summary>
        /// Default sender configuration
        /// </summary>
        public EmailSender DefaultSender { get; set; } = new();

        /// <summary>
        /// Request timeout in seconds (default: 30)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// General email configuration settings
    /// </summary>
    public class GeneralEmailSettings
    {
        /// <summary>
        /// Enable or disable email sending (useful for testing)
        /// </summary>
        public bool EnableEmailSending { get; set; } = true;

        /// <summary>
        /// Log email content to console/logs (for debugging)
        /// </summary>
        public bool LogEmailContent { get; set; } = false;

        /// <summary>
        /// Use sandbox mode (emails won't actually be sent)
        /// </summary>
        public bool UseSandboxMode { get; set; } = false;

        /// <summary>
        /// Maximum retry attempts for failed emails
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Delay between retry attempts in seconds
        /// </summary>
        public int RetryDelaySeconds { get; set; } = 5;
    }

    /// <summary>
    /// Email template configuration settings
    /// </summary>
    public class TemplateSettings
    {
        /// <summary>
        /// Base directory for email templates
        /// </summary>
        public string TemplateDirectory { get; set; } = "Templates/Email";

        /// <summary>
        /// Default template language (e.g., "en", "es", "fr")
        /// </summary>
        public string DefaultLanguage { get; set; } = "en";

        /// <summary>
        /// Company/brand information for templates
        /// </summary>
        public BrandInfo Brand { get; set; } = new();
    }

    /// <summary>
    /// Email sender information
    /// </summary>
    public class EmailSender
    {
        /// <summary>
        /// Sender email address
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Sender display name
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Brand information for email templates
    /// </summary>
    public class BrandInfo
    {
        /// <summary>
        /// Company/application name
        /// </summary>
        public string CompanyName { get; set; } = "StartEvent";

        /// <summary>
        /// Company website URL
        /// </summary>
        public string WebsiteUrl { get; set; } = "https://startevent.com";

        /// <summary>
        /// Support email address
        /// </summary>
        public string SupportEmail { get; set; } = "support@startevent.com";

        /// <summary>
        /// Logo URL for email templates
        /// </summary>
        public string LogoUrl { get; set; } = string.Empty;

        /// <summary>
        /// Primary brand color (hex code)
        /// </summary>
        public string PrimaryColor { get; set; } = "#007bff";

        /// <summary>
        /// Secondary brand color (hex code)
        /// </summary>
        public string SecondaryColor { get; set; } = "#6c757d";
    }
}