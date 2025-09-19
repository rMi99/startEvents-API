using StartEvent_API.Data.Entities;

namespace StartEvent_API.Models.Email
{
    /// <summary>
    /// Base email template model containing common properties
    /// </summary>
    public abstract class EmailTemplateBase
    {
        /// <summary>
        /// Email recipient information
        /// </summary>
        public EmailRecipient To { get; set; } = new();

        /// <summary>
        /// Email subject line
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Template type identifier
        /// </summary>
        public EmailTemplateType TemplateType { get; set; }

        /// <summary>
        /// Template language code (e.g., "en", "es", "fr")
        /// </summary>
        public string Language { get; set; } = "en";

        /// <summary>
        /// Additional custom data for template rendering
        /// </summary>
        public Dictionary<string, object> CustomData { get; set; } = new();
    }

    /// <summary>
    /// Email recipient information
    /// </summary>
    public class EmailRecipient
    {
        /// <summary>
        /// Recipient email address
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Recipient display name
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Email template types enumeration
    /// </summary>
    public enum EmailTemplateType
    {
        Welcome,
        TicketConfirmation,
        PaymentConfirmation,
        EventReminder,
        EventCancellation,
        PasswordReset,
        EmailVerification,
        EventUpdate,
        RefundConfirmation,
        OrganizerInvitation
    }

    /// <summary>
    /// Welcome email template model for new user registration
    /// </summary>
    public class WelcomeEmailTemplate : EmailTemplateBase
    {
        public WelcomeEmailTemplate()
        {
            TemplateType = EmailTemplateType.Welcome;
            Subject = "Welcome to StartEvent!";
        }

        /// <summary>
        /// User information
        /// </summary>
        public ApplicationUser User { get; set; } = new();

        /// <summary>
        /// Verification link for email confirmation
        /// </summary>
        public string VerificationLink { get; set; } = string.Empty;

        /// <summary>
        /// Dashboard/profile link
        /// </summary>
        public string DashboardLink { get; set; } = string.Empty;
    }

    /// <summary>
    /// Ticket confirmation email template model
    /// </summary>
    public class TicketConfirmationEmailTemplate : EmailTemplateBase
    {
        public TicketConfirmationEmailTemplate()
        {
            TemplateType = EmailTemplateType.TicketConfirmation;
            Subject = "Your Event Ticket Confirmation";
        }

        /// <summary>
        /// Ticket information
        /// </summary>
        public Ticket Ticket { get; set; } = new();

        /// <summary>
        /// Event information
        /// </summary>
        public Event Event { get; set; } = new();

        /// <summary>
        /// QR code image as base64 string
        /// </summary>
        public string QrCodeBase64 { get; set; } = string.Empty;

        /// <summary>
        /// Ticket download link
        /// </summary>
        public string TicketDownloadLink { get; set; } = string.Empty;

        /// <summary>
        /// Event details page link
        /// </summary>
        public string EventDetailsLink { get; set; } = string.Empty;

        /// <summary>
        /// Venue information
        /// </summary>
        public Venue Venue { get; set; } = new();
    }

    /// <summary>
    /// Payment confirmation email template model
    /// </summary>
    public class PaymentConfirmationEmailTemplate : EmailTemplateBase
    {
        public PaymentConfirmationEmailTemplate()
        {
            TemplateType = EmailTemplateType.PaymentConfirmation;
            Subject = "Payment Confirmation";
        }

        /// <summary>
        /// Payment information
        /// </summary>
        public Payment Payment { get; set; } = new();

        /// <summary>
        /// Associated ticket information
        /// </summary>
        public Ticket Ticket { get; set; } = new();

        /// <summary>
        /// Event information
        /// </summary>
        public Event Event { get; set; } = new();

        /// <summary>
        /// Receipt/invoice download link
        /// </summary>
        public string ReceiptDownloadLink { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event reminder email template model
    /// </summary>
    public class EventReminderEmailTemplate : EmailTemplateBase
    {
        public EventReminderEmailTemplate()
        {
            TemplateType = EmailTemplateType.EventReminder;
            Subject = "Event Reminder: Don't Miss Out!";
        }

        /// <summary>
        /// Event information
        /// </summary>
        public Event Event { get; set; } = new();

        /// <summary>
        /// User's tickets for this event
        /// </summary>
        public List<Ticket> UserTickets { get; set; } = new();

        /// <summary>
        /// Time until event starts (e.g., "2 hours", "1 day")
        /// </summary>
        public string TimeUntilEvent { get; set; } = string.Empty;

        /// <summary>
        /// Venue information
        /// </summary>
        public Venue Venue { get; set; } = new();

        /// <summary>
        /// Directions/map link
        /// </summary>
        public string DirectionsLink { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event cancellation email template model
    /// </summary>
    public class EventCancellationEmailTemplate : EmailTemplateBase
    {
        public EventCancellationEmailTemplate()
        {
            TemplateType = EmailTemplateType.EventCancellation;
            Subject = "Event Cancelled - Refund Information";
        }

        /// <summary>
        /// Cancelled event information
        /// </summary>
        public Event Event { get; set; } = new();

        /// <summary>
        /// User's affected tickets
        /// </summary>
        public List<Ticket> AffectedTickets { get; set; } = new();

        /// <summary>
        /// Cancellation reason
        /// </summary>
        public string CancellationReason { get; set; } = string.Empty;

        /// <summary>
        /// Refund information
        /// </summary>
        public RefundInfo RefundDetails { get; set; } = new();

        /// <summary>
        /// Support contact information
        /// </summary>
        public string SupportContactInfo { get; set; } = string.Empty;
    }

    /// <summary>
    /// Password reset email template model
    /// </summary>
    public class PasswordResetEmailTemplate : EmailTemplateBase
    {
        public PasswordResetEmailTemplate()
        {
            TemplateType = EmailTemplateType.PasswordReset;
            Subject = "Reset Your Password";
        }

        /// <summary>
        /// User information
        /// </summary>
        public ApplicationUser User { get; set; } = new();

        /// <summary>
        /// Password reset link with token
        /// </summary>
        public string ResetLink { get; set; } = string.Empty;

        /// <summary>
        /// Link expiration time
        /// </summary>
        public DateTime LinkExpiresAt { get; set; }

        /// <summary>
        /// Request IP address (for security)
        /// </summary>
        public string RequestIpAddress { get; set; } = string.Empty;
    }

    /// <summary>
    /// Email verification template model
    /// </summary>
    public class EmailVerificationEmailTemplate : EmailTemplateBase
    {
        public EmailVerificationEmailTemplate()
        {
            TemplateType = EmailTemplateType.EmailVerification;
            Subject = "Verify Your Email Address";
        }

        /// <summary>
        /// User information
        /// </summary>
        public ApplicationUser User { get; set; } = new();

        /// <summary>
        /// Email verification link
        /// </summary>
        public string VerificationLink { get; set; } = string.Empty;

        /// <summary>
        /// Verification code (alternative to link)
        /// </summary>
        public string VerificationCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event update notification email template model
    /// </summary>
    public class EventUpdateEmailTemplate : EmailTemplateBase
    {
        public EventUpdateEmailTemplate()
        {
            TemplateType = EmailTemplateType.EventUpdate;
            Subject = "Important Event Update";
        }

        /// <summary>
        /// Updated event information
        /// </summary>
        public Event Event { get; set; } = new();

        /// <summary>
        /// Update details/message
        /// </summary>
        public string UpdateMessage { get; set; } = string.Empty;

        /// <summary>
        /// List of changes made
        /// </summary>
        public List<string> Changes { get; set; } = new();

        /// <summary>
        /// User's tickets for this event
        /// </summary>
        public List<Ticket> UserTickets { get; set; } = new();
    }

    /// <summary>
    /// Organizer invitation email template model
    /// </summary>
    public class OrganizerInvitationEmailTemplate : EmailTemplateBase
    {
        public OrganizerInvitationEmailTemplate()
        {
            TemplateType = EmailTemplateType.OrganizerInvitation;
            Subject = "Invitation to Become an Event Organizer";
        }

        /// <summary>
        /// Invitee information
        /// </summary>
        public string InviteeEmail { get; set; } = string.Empty;

        /// <summary>
        /// Invitee name
        /// </summary>
        public string InviteeName { get; set; } = string.Empty;

        /// <summary>
        /// Inviter (admin) information
        /// </summary>
        public ApplicationUser InvitedBy { get; set; } = new();

        /// <summary>
        /// Invitation acceptance link
        /// </summary>
        public string InvitationLink { get; set; } = string.Empty;

        /// <summary>
        /// Personal invitation message
        /// </summary>
        public string PersonalMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Refund information model
    /// </summary>
    public class RefundInfo
    {
        /// <summary>
        /// Total refund amount
        /// </summary>
        public decimal RefundAmount { get; set; }

        /// <summary>
        /// Refund method (e.g., "Credit Card", "Bank Transfer")
        /// </summary>
        public string RefundMethod { get; set; } = string.Empty;

        /// <summary>
        /// Expected processing time
        /// </summary>
        public string ProcessingTime { get; set; } = "3-5 business days";

        /// <summary>
        /// Refund reference number
        /// </summary>
        public string ReferenceNumber { get; set; } = string.Empty;
    }

    /// <summary>
    /// Email template rendering result
    /// </summary>
    public class EmailTemplateResult
    {
        /// <summary>
        /// Rendered HTML content
        /// </summary>
        public string HtmlContent { get; set; } = string.Empty;

        /// <summary>
        /// Plain text content (fallback)
        /// </summary>
        public string TextContent { get; set; } = string.Empty;

        /// <summary>
        /// Final email subject
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Any attachments to include
        /// </summary>
        public List<EmailAttachment> Attachments { get; set; } = new();
    }

    /// <summary>
    /// Email attachment model
    /// </summary>
    public class EmailAttachment
    {
        /// <summary>
        /// Attachment filename
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// File content as byte array
        /// </summary>
        public byte[] Content { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// MIME content type
        /// </summary>
        public string ContentType { get; set; } = "application/octet-stream";

        /// <summary>
        /// Attachment disposition (inline or attachment)
        /// </summary>
        public string Disposition { get; set; } = "attachment";
    }
}