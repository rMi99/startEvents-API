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

        public ICollection<Ticket>? Tickets { get; set; }
        public ICollection<Event>? OrganizedEvents { get; set; }
    }
}
