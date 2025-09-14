namespace StartEvent_API.Data.Entities
{
    public class LoyaltyPoint
    {
        public Guid Id { get; set; }
        public string CustomerId { get; set; } = default!;
        public ApplicationUser Customer { get; set; } = default!;
        public int Points { get; set; }
        public DateTime EarnedDate { get; set; } = DateTime.UtcNow; // Changed from LastUpdated
        public string Description { get; set; } = default!; // Added description
    }
}
