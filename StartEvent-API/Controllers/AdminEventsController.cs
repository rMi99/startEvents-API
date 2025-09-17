using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StartEvent_API.Data;
using StartEvent_API.Data.Entities;
using StartEvent_API.Models;
using StartEvent_API.Helper;

namespace StartEvent_API.Controllers
{
    [ApiController]
    [Route("api/admin/events")]
    [Authorize(Roles = "Admin")]
    public class AdminEventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorage _fileStorage;

        public AdminEventsController(ApplicationDbContext context, IFileStorage fileStorage)
        {
            _context = context;
            _fileStorage = fileStorage;
        }

        /// <summary>
        /// Gets all events with comprehensive filtering for admin users
        /// </summary>
        /// <param name="fromDate">Filter events from this date onwards</param>
        /// <param name="toDate">Filter events up to this date</param>
        /// <param name="category">Filter by event category</param>
        /// <param name="venue">Filter by venue name (partial match)</param>
        /// <param name="venueId">Filter by specific venue ID</param>
        /// <param name="keyword">Search in event title or description</param>
        /// <param name="isPublished">Filter by published status</param>
        /// <param name="organizerId">Filter by specific organizer ID</param>
        /// <param name="organizerName">Filter by organizer name (partial match)</param>
        /// <returns>List of all events with organizer information</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllEvents(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? category,
            [FromQuery] string? venue,
            [FromQuery] Guid? venueId,
            [FromQuery] string? keyword,
            [FromQuery] bool? isPublished,
            [FromQuery] string? organizerId,
            [FromQuery] string? organizerName)
        {
            var query = _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .AsQueryable();

            // Apply filters (same as organizer but without organizer restriction)
            if (fromDate.HasValue)
                query = query.Where(e => e.EventDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(e => e.EventDate <= toDate.Value);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(e => EF.Functions.Like(e.Category.ToLower(), category.ToLower()));

            if (!string.IsNullOrEmpty(venue))
                query = query.Where(e => e.Venue.Name.Contains(venue));

            if (venueId.HasValue)
                query = query.Where(e => e.VenueId == venueId.Value);

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(e => e.Title.Contains(keyword) || e.Description.Contains(keyword));

            if (isPublished.HasValue)
                query = query.Where(e => e.IsPublished == isPublished.Value);

            // Admin-specific filters
            if (!string.IsNullOrEmpty(organizerId))
                query = query.Where(e => e.OrganizerId == organizerId);

            if (!string.IsNullOrEmpty(organizerName))
                query = query.Where(e => e.Organizer.FullName != null && e.Organizer.FullName.Contains(organizerName));

            var events = await query.OrderBy(e => e.EventDate).ToListAsync();

            var adminEventDtos = events.Select(e => new AdminEventDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                EventDate = e.EventDate,
                EventTime = e.EventTime,
                Category = e.Category,
                Image = e.Image,
                ImageUrl = !string.IsNullOrEmpty(e.Image) ? _fileStorage.GetFileUrlAsync(e.Image).Result : null,
                IsPublished = e.IsPublished,
                VenueId = e.VenueId,
                VenueName = e.Venue != null ? e.Venue.Name ?? string.Empty : string.Empty,
                OrganizerId = e.OrganizerId,
                OrganizerName = e.Organizer?.FullName ?? string.Empty,
                OrganizerEmail = e.Organizer?.Email,
                OrganizationName = e.Organizer?.OrganizationName,
                CreatedAt = e.CreatedAt,
                ModifiedAt = e.ModifiedAt
            }).ToList();

            return Ok(adminEventDtos);
        }

        /// <summary>
        /// Gets a specific event by ID with full details for admin
        /// </summary>
        /// <param name="id">Event ID</param>
        /// <returns>Event details with organizer information</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEventById(Guid id)
        {
            var eventEntity = await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .Include(e => e.Prices)
                .Include(e => e.Tickets)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventEntity == null)
            {
                return NotFound("Event not found");
            }

            var adminEventDto = new AdminEventDto
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                EventDate = eventEntity.EventDate,
                EventTime = eventEntity.EventTime,
                Category = eventEntity.Category,
                Image = eventEntity.Image,
                ImageUrl = !string.IsNullOrEmpty(eventEntity.Image) ? _fileStorage.GetFileUrlAsync(eventEntity.Image).Result : null,
                IsPublished = eventEntity.IsPublished,
                VenueId = eventEntity.VenueId,
                VenueName = eventEntity.Venue?.Name ?? string.Empty,
                OrganizerId = eventEntity.OrganizerId,
                OrganizerName = eventEntity.Organizer?.FullName ?? string.Empty,
                OrganizerEmail = eventEntity.Organizer?.Email,
                OrganizationName = eventEntity.Organizer?.OrganizationName,
                CreatedAt = eventEntity.CreatedAt,
                ModifiedAt = eventEntity.ModifiedAt
            };

            return Ok(adminEventDto);
        }

        /// <summary>
        /// Updates event publication status (Admin can publish/unpublish any event)
        /// </summary>
        /// <param name="id">Event ID</param>
        /// <param name="isPublished">New publication status</param>
        /// <returns>Updated event</returns>
        [HttpPut("{id}/publish")]
        public async Task<IActionResult> UpdateEventPublicationStatus(Guid id, [FromBody] bool isPublished)
        {
            var eventEntity = await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventEntity == null)
            {
                return NotFound("Event not found");
            }

            eventEntity.IsPublished = isPublished;
            eventEntity.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var adminEventDto = new AdminEventDto
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                EventDate = eventEntity.EventDate,
                EventTime = eventEntity.EventTime,
                Category = eventEntity.Category,
                Image = eventEntity.Image,
                ImageUrl = !string.IsNullOrEmpty(eventEntity.Image) ? _fileStorage.GetFileUrlAsync(eventEntity.Image).Result : null,
                IsPublished = eventEntity.IsPublished,
                VenueId = eventEntity.VenueId,
                VenueName = eventEntity.Venue?.Name ?? string.Empty,
                OrganizerId = eventEntity.OrganizerId,
                OrganizerName = eventEntity.Organizer?.FullName ?? string.Empty,
                OrganizerEmail = eventEntity.Organizer?.Email,
                OrganizationName = eventEntity.Organizer?.OrganizationName,
                CreatedAt = eventEntity.CreatedAt,
                ModifiedAt = eventEntity.ModifiedAt
            };

            return Ok(adminEventDto);
        }

        /// <summary>
        /// Deletes an event (Admin can delete any event)
        /// </summary>
        /// <param name="id">Event ID</param>
        /// <returns>Success confirmation</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(Guid id)
        {
            var eventEntity = await _context.Events
                .Include(e => e.Tickets)
                .Include(e => e.Prices)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventEntity == null)
            {
                return NotFound("Event not found");
            }

            // Check if event has sold tickets
            if (eventEntity.Tickets.Any(t => t.IsPaid))
            {
                return BadRequest("Cannot delete event with sold tickets. Consider unpublishing instead.");
            }

            // Remove related data
            _context.EventPrices.RemoveRange(eventEntity.Prices);
            _context.Tickets.RemoveRange(eventEntity.Tickets);
            _context.Events.Remove(eventEntity);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Event deleted successfully" });
        }

        /// <summary>
        /// Gets event statistics for admin dashboard
        /// </summary>
        /// <returns>Event statistics</returns>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetEventStatistics()
        {
            var stats = new
            {
                TotalEvents = await _context.Events.CountAsync(),
                PublishedEvents = await _context.Events.CountAsync(e => e.IsPublished),
                UnpublishedEvents = await _context.Events.CountAsync(e => !e.IsPublished),
                UpcomingEvents = await _context.Events.CountAsync(e => e.EventDate >= DateTime.UtcNow),
                PastEvents = await _context.Events.CountAsync(e => e.EventDate < DateTime.UtcNow),
                EventsByCategory = await _context.Events
                    .GroupBy(e => e.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToListAsync(),
                TopOrganizers = await _context.Events
                    .Include(e => e.Organizer)
                    .GroupBy(e => new { e.OrganizerId, e.Organizer.FullName })
                    .Select(g => new
                    {
                        OrganizerId = g.Key.OrganizerId,
                        OrganizerName = g.Key.FullName,
                        EventCount = g.Count()
                    })
                    .OrderByDescending(x => x.EventCount)
                    .Take(10)
                    .ToListAsync()
            };

            return Ok(stats);
        }
    }
}
