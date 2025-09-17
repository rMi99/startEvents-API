namespace StartEvent_API.Models
{
    public class AdminEventDto
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public DateTime EventDate { get; set; }
        public DateTime EventTime { get; set; }
        public string Category { get; set; } = default!;
        public string? Image { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsPublished { get; set; }
        public Guid VenueId { get; set; }
        public required string VenueName { get; set; }

        // Admin-specific fields
        public required string OrganizerId { get; set; }
        public required string OrganizerName { get; set; }
        public string? OrganizerEmail { get; set; }
        public string? OrganizationName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}