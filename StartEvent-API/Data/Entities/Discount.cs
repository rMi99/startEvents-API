namespace StartEvent_API.Data.Entities
{
    public class Discount
    {
        public Guid Id { get; set; }
        public Guid? EventId { get; set; }
        public Event? Event { get; set; }
        public string Code { get; set; } = default!;
        public string Type { get; set; } = "Percent"; // Percent, Amount
        public decimal Value { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool IsActive { get; set; } = true;

    }
}
