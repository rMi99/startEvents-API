namespace StartEvent_API.Data.Entities
{
    public class Payment
    {
        public Guid Id { get; set; }
        public string CustomerId { get; set; } = default!;
        public ApplicationUser Customer { get; set; } = default!;
        public Guid TicketId { get; set; }
        public Ticket Ticket { get; set; } = default!;
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = default!; // "Pending", "Completed", "Failed"
        public string PaymentMethod { get; set; } = default!; // "Card", "Cash", "Online"
        public string? TransactionId { get; set; }
    }
}
