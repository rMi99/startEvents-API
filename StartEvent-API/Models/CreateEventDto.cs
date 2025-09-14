using StartEvent_API.Data.Entities;

namespace StartEvent_API.Models
{
    public class CreateEventDto
    {
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public DateTime EventDate { get; set; }
        public DateTime EventTime { get; set; }
        public string Category { get; set; } = default!;
        public string? Image { get; set; }
        public Guid VenueId { get; set; }
        public bool IsPublished { get; set; } = false;
        public List<CreateEventPriceDto>? Prices { get; set; }
    }

    public class CreateEventPriceDto
    {
        public string Name { get; set; } = default!;
        public decimal Price { get; set; }
        public int AvailableTickets { get; set; }
    }
}