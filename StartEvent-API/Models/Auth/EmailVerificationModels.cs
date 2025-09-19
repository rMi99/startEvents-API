using System.ComponentModel.DataAnnotations;

namespace StartEvent_API.Models.Auth
{
    /// <summary>
    /// Request model for sending email verification code
    /// </summary>
    public class SendEmailVerificationRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for verifying email with code
    /// </summary>
    public class VerifyEmailRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6)]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Verification code must be exactly 6 digits")]
        public string VerificationCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response model for email verification operations
    /// </summary>
    public class EmailVerificationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Response model for resend verification code
    /// </summary>
    public class ResendVerificationResponse : EmailVerificationResponse
    {
        public int? RemainingAttempts { get; set; }
        public TimeSpan? RetryAfter { get; set; }
    }

    /// <summary>
    /// Request model for resending email verification code
    /// </summary>
    public class ResendEmailVerificationRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response model for email verification
    /// </summary>
    public class VerifyEmailResponse : EmailVerificationResponse
    {
        public bool IsFirstTimeVerification { get; set; }
    }

    /// <summary>
    /// Internal model for verification code details
    /// </summary>
    public class VerificationCodeDetails
    {
        public string Code { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }
}