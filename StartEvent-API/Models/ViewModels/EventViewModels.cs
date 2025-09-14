namespace StartEvent_API.Models.ViewModels
{
    public class EventViewModel
    {
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required string VenueName { get; set; }
        public DateTime EventDate { get; set; }
        public Guid VenueId { get; set; }
        public string? Category { get; set; }
        public string? Image { get; set; }
        public string? OrganizerId { get; set; }
    }

    public class CreateEventViewModel
    {
        public required string Title { get; set; }
        public required string Description { get; set; }
        public DateTime EventDate { get; set; }
        public DateTime EventTime { get; set; }
        public required string Category { get; set; }
        public Guid VenueId { get; set; }
        public string? Image { get; set; }
    }

    public class UpdateEventViewModel
    {
        public required string Title { get; set; }
        public required string Description { get; set; }
        public DateTime EventDate { get; set; }
        public DateTime EventTime { get; set; }
        public required string Category { get; set; }
        public Guid VenueId { get; set; }
        public string? Image { get; set; }
        public bool IsPublished { get; set; }
    }
}
