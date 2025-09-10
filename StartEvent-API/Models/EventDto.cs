namespace StartEvent_API.Models
{
    public class EventDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime EventDate { get; set; }
        public Guid VenueId { get; set; }
        public string VenueName { get; set; }
    }
}
