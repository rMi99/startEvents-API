namespace StartEvent_API.Models
{
    public class UpdateEventDto
    {
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public DateTime EventDate { get; set; }
        public DateTime EventTime { get; set; }
        public string Category { get; set; } = default!;
        public string? Image { get; set; }
        public Guid VenueId { get; set; }
        public bool IsPublished { get; set; }
    }
}