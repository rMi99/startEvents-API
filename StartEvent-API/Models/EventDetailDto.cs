namespace StartEvent_API.Models
{
    public class EventDetailDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public DateTime EventDate { get; set; }
        public DateTime EventTime { get; set; }
        public string Category { get; set; } = default!;
        public string? Image { get; set; }
        public bool IsPublished { get; set; }
        public Guid VenueId { get; set; }
        public string VenueName { get; set; } = default!;
        public string? VenueAddress { get; set; }
        public string OrganizerName { get; set; } = default!;
        public string? OrganizerContact { get; set; }
        public List<EventPriceDto>? Prices { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EventPriceDto
    {
        public Guid Id { get; set; }
        public string Category { get; set; } = default!;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
    }
}