namespace StartEvent_API.Models
{
    public class OrganizerDto
    {
        public required string Id { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public string? UserName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? OrganizationName { get; set; }
        public string? OrganizationContact { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }

        // Statistics
        public int TotalEvents { get; set; }
        public int PublishedEvents { get; set; }
        public int UnpublishedEvents { get; set; }
        public int UpcomingEvents { get; set; }
        public int PastEvents { get; set; }
    }

    public class OrganizerDetailDto : OrganizerDto
    {
        public List<AdminEventDto> RecentEvents { get; set; } = new List<AdminEventDto>();
        public List<string> EventCategories { get; set; } = new List<string>();
        public DateTime? FirstEventDate { get; set; }
        public DateTime? LastEventDate { get; set; }
    }
}