using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace StartEvent_API.Data.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? OrganizationName { get; set; }
        public string? OrganizationContact { get; set; }
        public DateTime? LastLogin { get; set; }

        // Email verification properties
        public bool IsEmailVerified { get; set; } = false;
        public string? EmailVerificationCode { get; set; }
        public DateTime? EmailVerificationCodeExpiry { get; set; }

        // Password reset OTP properties
        public string? PasswordResetOtp { get; set; }
        public DateTime? PasswordResetOtpExpiry { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }
        public int PasswordResetAttempts { get; set; } = 0;
        public DateTime? PasswordResetLastAttempt { get; set; }
        // Loyalty Points - computed property for easy access
        public int LoyaltyPoints => LoyaltyPointsHistory?.Sum(lp => lp.Points) ?? 0;

        public ICollection<Ticket>? Tickets { get; set; }
        public ICollection<Event>? OrganizedEvents { get; set; }
        public ICollection<LoyaltyPoint>? LoyaltyPointsHistory { get; set; }
    }
}
