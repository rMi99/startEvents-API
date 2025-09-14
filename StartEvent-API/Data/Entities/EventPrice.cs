namespace StartEvent_API.Data.Entities
{
    public class EventPrice
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public Event Event { get; set; } = default!;
        public string Category { get; set; } = default!;
        public int Stock { get; set; }
        public bool IsActive { get; set; } = true;
        public decimal Price { get; set; }

    }
}
