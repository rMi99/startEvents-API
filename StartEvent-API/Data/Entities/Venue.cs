using StartEvent_API.Helper;

namespace StartEvent_API.Data.Entities
{
    public class Venue : CommonProps
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Location { get; set; } = default!;
        public int Capacity { get; set; }

        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
