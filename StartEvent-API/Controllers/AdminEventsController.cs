using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StartEvent_API.Data;
using StartEvent_API.Data.Entities;

namespace StartEvent_API.Controllers
{
    [ApiController]
    [Route("api/admin/events")]
    [Authorize(Roles = "Admin")]
    public class AdminEventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminEventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/events
        [HttpGet]
        public async Task<IActionResult> GetAllEvents()
        {
            var events = await _context.Events.Include(e => e.Venue).ToListAsync();
            return Ok(events);
        }

        // PUT: api/admin/events/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] Event updatedEvent)
        {
            var existingEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
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

        // DELETE: api/admin/events/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(Guid id)
        {
            var existingEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (existingEvent == null)
                return NotFound();

            _context.Events.Remove(existingEvent);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
