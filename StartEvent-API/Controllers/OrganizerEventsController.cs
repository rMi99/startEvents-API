using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StartEvent_API.Data;
using StartEvent_API.Data.Entities;
using System.Security.Claims;
using StartEvent_API.Models;

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
            var eventDtos = events.Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                EventDate = e.EventDate,
                VenueId = e.VenueId,
                VenueName = e.Venue != null ? e.Venue.Name ?? string.Empty : string.Empty
            }).ToList();
            return Ok(eventDtos);
        }

        // POST: api/organizer/events
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto createEventDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(organizerId))
            {
                return Unauthorized("User not authenticated");
            }

            // Create the event entity
            var newEvent = new Event
            {
                Id = Guid.NewGuid(),
                Title = createEventDto.Title,
                Description = createEventDto.Description,
                EventDate = createEventDto.EventDate,
                EventTime = createEventDto.EventTime,
                Category = createEventDto.Category,
                Image = createEventDto.Image,
                VenueId = createEventDto.VenueId,
                IsPublished = createEventDto.IsPublished,
                OrganizerId = organizerId,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            // Add event prices if provided
            if (createEventDto.Prices != null && createEventDto.Prices.Any())
            {
                foreach (var priceDto in createEventDto.Prices)
                {
                    var eventPrice = new EventPrice
                    {
                        Id = Guid.NewGuid(),
                        EventId = newEvent.Id,
                        Category = priceDto.Name,
                        Price = priceDto.Price,
                        Stock = priceDto.AvailableTickets,
                        IsActive = true
                    };
                    newEvent.Prices.Add(eventPrice);
                }
            }

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            var eventDto = new EventDto
            {
                Id = newEvent.Id,
                Title = newEvent.Title,
                Description = newEvent.Description,
                EventDate = newEvent.EventDate,
                VenueId = newEvent.VenueId,
                VenueName = newEvent.Venue != null ? newEvent.Venue.Name ?? string.Empty : string.Empty
            };

            return CreatedAtAction(nameof(GetMyEvents), new { id = newEvent.Id }, eventDto);
        }

        // PUT: api/organizer/events/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventDto updateEventDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(organizerId))
            {
                return Unauthorized("User not authenticated");
            }

            var existingEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == organizerId);
            if (existingEvent == null)
                return NotFound("Event not found or you don't have permission to update it");

            // Update event properties
            existingEvent.Title = updateEventDto.Title;
            existingEvent.Description = updateEventDto.Description;
            existingEvent.EventDate = updateEventDto.EventDate;
            existingEvent.EventTime = updateEventDto.EventTime;
            existingEvent.Category = updateEventDto.Category;
            existingEvent.Image = updateEventDto.Image;
            existingEvent.VenueId = updateEventDto.VenueId;
            existingEvent.IsPublished = updateEventDto.IsPublished;
            existingEvent.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
