using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StartEvent_API.Data;
using StartEvent_API.Data.Entities;
using System.Security.Claims;

namespace StartEvent_API.Controllers
{
    [ApiController]
    [Route("api/organizer/events")]
    [Authorize(Roles = "Organizer")]
    public class OrganizerEventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrganizerEventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/organizer/events
        [HttpGet]
        public async Task<IActionResult> GetMyEvents()
        {
            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var events = await _context.Events
                .Where(e => e.OrganizerId == organizerId)
                .Include(e => e.Venue)
                .ToListAsync();
            return Ok(events);
        }

        // POST: api/organizer/events
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] Event newEvent)
        {
            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            newEvent.OrganizerId = organizerId;
            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetMyEvents), new { id = newEvent.Id }, newEvent);
        }

        // PUT: api/organizer/events/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] Event updatedEvent)
        {
            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == organizerId);
            if (existingEvent == null)
                return NotFound();

            existingEvent.Title = updatedEvent.Title;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.EventDate = updatedEvent.EventDate;
            existingEvent.VenueId = updatedEvent.VenueId;
            // Add other updatable fields as needed

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
