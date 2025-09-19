using System;

namespace StartEvent_API.Models
{
    public class TicketHistoryDto
    {
        public Guid Id { get; set; }
        public string? CustomerId { get; set; }
        public Guid EventId { get; set; }
        public Guid EventPriceId { get; set; }
        public string? TicketNumber { get; set; }
        public string? TicketCode { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PurchaseDate { get; set; }
        public bool IsPaid { get; set; }
        public string? QrCodePath { get; set; }
        
        // Event details (without circular references)
        public EventSummaryDto? Event { get; set; }
        public EventPriceSummaryDto? EventPrice { get; set; }
    }

    public class EventSummaryDto
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string? EventTime { get; set; }
        public string? Category { get; set; }
        public string? Image { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsPublished { get; set; }
        
        // Venue details (without circular references)
        public VenueSummaryDto? Venue { get; set; }
    }

    public class VenueSummaryDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Location { get; set; }
        public int Capacity { get; set; }
    }

    public class EventPriceSummaryDto
    {
        public Guid Id { get; set; }
        public string? Category { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
    }
}

