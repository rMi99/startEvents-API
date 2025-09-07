namespace StartEvent_API.Data.Entities
{
    public class Ticket
    {
        public Guid Id { get; set; }

        public string CustomerId { get; set; } = default!;
        public ApplicationUser Customer { get; set; } = default!;

        public Guid EventId { get; set; }
        public Event Event { get; set; } = default!;

        public Guid EventPriceId { get; set; }
        public EventPrice EventPrice { get; set; } = default!;

        public string TicketNumber { get; set; } = default!;
        public string TicketCode { get; set; } = default!; // Added for ticket code
        public int Quantity { get; set; } = 1; // Added for quantity
        public decimal TotalAmount { get; set; } // Added for total amount
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        public bool IsPaid { get; set; } = false;
        public string QrCodePath { get; set; } = default!;
        
        // Loyalty Points tracking
        public int PointsEarned { get; set; } = 0;
        public int PointsRedeemed { get; set; } = 0;
    }
}
