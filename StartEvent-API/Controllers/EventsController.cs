using Microsoft.AspNetCore.Mvc;
using StartEvent_API.Data.Entities;
using StartEvent_API.Data;
using Microsoft.EntityFrameworkCore;
using StartEvent_API.Models;

namespace StartEvent_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/events
        [HttpGet]
        public async Task<IActionResult> GetUpcomingEvents([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] string? venue, [FromQuery] string? keyword)
        {
            var query = _context.Events.AsQueryable();

            // Only upcoming events
            query = query.Where(e => e.EventDate >= DateTime.UtcNow);

            if (fromDate.HasValue)
                query = query.Where(e => e.EventDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(e => e.EventDate <= toDate.Value);
            if (!string.IsNullOrEmpty(venue))
                query = query.Where(e => e.Venue.Name.Contains(venue));
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(e => e.Title.Contains(keyword) || e.Description.Contains(keyword));

            var events = await query.Include(e => e.Venue).ToListAsync();
            var eventDtos = events.Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                EventDate = e.EventDate,
                VenueId = e.VenueId,
                VenueName = e.Venue != null ? e.Venue.Name : null
            }).ToList();
            return Ok(eventDtos);
        }
    }
}
