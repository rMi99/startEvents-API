namespace StartEvent_API.Models
{
    public class EventResponseDto
    {
        public string createdAt { get; set; } = default!;
        public string? modifiedAt { get; set; }
        public string? deletedAt { get; set; }
        public string id { get; set; } = default!;
        public string venueId { get; set; } = default!;
        public VenueResponseDto venue { get; set; } = default!;
        public string? organizerId { get; set; }
        public UserResponseDto organizer { get; set; } = default!;
        public string? title { get; set; }
        public string? description { get; set; }
        public string eventDate { get; set; } = default!;
        public string eventTime { get; set; } = default!;
        public string? category { get; set; }
        public string? image { get; set; }
        public bool isPublished { get; set; }
        public List<TicketResponseDto>? tickets { get; set; }
        public List<EventPriceResponseDto>? prices { get; set; }
    }

    public class VenueResponseDto
    {
        public string id { get; set; } = default!;
        public string? name { get; set; }
        public string? location { get; set; }
        public int capacity { get; set; }
    }

    public class UserResponseDto
    {
        public string id { get; set; } = default!;
        public string? fullName { get; set; }
        public string? email { get; set; }
        public string? address { get; set; }
        public string? organizationName { get; set; }
        public string? organizationContact { get; set; }
        public bool isActive { get; set; }
    }

    public class EventPriceResponseDto
    {
        public string id { get; set; } = default!;
        public string eventId { get; set; } = default!;
        public string category { get; set; } = default!;
        public int stock { get; set; }
        public bool isActive { get; set; }
        public decimal price { get; set; }
    }

    public class TicketResponseDto
    {
        public string id { get; set; } = default!;
        public string customerId { get; set; } = default!;
        public string eventId { get; set; } = default!;
        public string eventPriceId { get; set; } = default!;
        public string ticketNumber { get; set; } = default!;
        public string ticketCode { get; set; } = default!;
        public int quantity { get; set; }
        public decimal totalAmount { get; set; }
        public string purchaseDate { get; set; } = default!;
        public bool isPaid { get; set; }
        public string qrCodePath { get; set; } = default!;
    }
}