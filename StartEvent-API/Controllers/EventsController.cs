using Microsoft.AspNetCore.Mvc;
using StartEvent_API.Data.Entities;
using StartEvent_API.Data;
using Microsoft.EntityFrameworkCore;
using StartEvent_API.Models;
using StartEvent_API.Helper;

namespace StartEvent_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorage _fileStorage;

        public EventsController(ApplicationDbContext context, IFileStorage fileStorage)
        {
            _context = context;
            _fileStorage = fileStorage;
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
                EventTime = e.EventTime,
                Category = e.Category,
                Image = e.Image,
                ImageUrl = !string.IsNullOrEmpty(e.Image) ? _fileStorage.GetFileUrlAsync(e.Image).Result : null,
                IsPublished = e.IsPublished,
                VenueId = e.VenueId,
                VenueName = e.Venue != null ? e.Venue.Name : null
            }).ToList();
            return Ok(eventDtos);
        }

        // GET: api/events/{id}
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

            var frontendEventDto = new EventResponseDto
            {
                createdAt = eventEntity.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                modifiedAt = eventEntity.ModifiedAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                deletedAt = eventEntity.DeletedAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                id = eventEntity.Id.ToString(),
                venueId = eventEntity.VenueId.ToString(),
                venue = new VenueResponseDto
                {
                    id = eventEntity.Venue?.Id.ToString() ?? "",
                    name = eventEntity.Venue?.Name,
                    location = eventEntity.Venue?.Location,
                    capacity = eventEntity.Venue?.Capacity ?? 0
                },
                organizerId = eventEntity.OrganizerId,
                organizer = new UserResponseDto
                {
                    id = eventEntity.Organizer?.Id ?? "",
                    fullName = eventEntity.Organizer?.FullName,
                    email = eventEntity.Organizer?.Email,
                    address = eventEntity.Organizer?.Address,
                    organizationName = eventEntity.Organizer?.OrganizationName,
                    organizationContact = eventEntity.Organizer?.OrganizationContact,
                    isActive = eventEntity.Organizer?.IsActive ?? false
                },
                title = eventEntity.Title,
                description = eventEntity.Description,
                eventDate = eventEntity.EventDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                eventTime = eventEntity.EventTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                category = eventEntity.Category,
                image = eventEntity.Image,
                isPublished = eventEntity.IsPublished,
                tickets = eventEntity.Tickets?.Select(t => new TicketResponseDto
                {
                    id = t.Id.ToString(),
                    customerId = t.CustomerId,
                    eventId = t.EventId.ToString(),
                    eventPriceId = t.EventPriceId.ToString(),
                    ticketNumber = t.TicketNumber,
                    ticketCode = t.TicketCode,
                    quantity = t.Quantity,
                    totalAmount = t.TotalAmount,
                    purchaseDate = t.PurchaseDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    isPaid = t.IsPaid,
                    qrCodePath = t.QrCodePath
                }).ToList(),
                prices = eventEntity.Prices?.Select(p => new EventPriceResponseDto
                {
                    id = p.Id.ToString(),
                    eventId = p.EventId.ToString(),
                    category = p.Category,
                    stock = p.Stock,
                    isActive = p.IsActive,
                    price = p.Price
                }).ToList()
            };

            return Ok(frontendEventDto);
        }
    }
}
