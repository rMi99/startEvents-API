namespace StartEvent_API.Models
{
    public class EventDto
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public DateTime EventDate { get; set; }
        public Guid VenueId { get; set; }
        public required string VenueName { get; set; }
    }
}
