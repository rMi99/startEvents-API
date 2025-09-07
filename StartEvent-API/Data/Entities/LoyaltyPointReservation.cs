namespace StartEvent_API.Data.Entities
{
    public class LoyaltyPointReservation
    {
        public Guid Id { get; set; }
        public string CustomerId { get; set; } = default!;
        public ApplicationUser Customer { get; set; } = default!;
        public Guid TicketId { get; set; }
        public Ticket Ticket { get; set; } = default!;
        public int ReservedPoints { get; set; }
        public DateTime ReservedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(30); // 30 minutes to complete payment
        public bool IsConfirmed { get; set; } = false;
        public bool IsExpired => DateTime.UtcNow > ExpiresAt && !IsConfirmed;
    }
}
