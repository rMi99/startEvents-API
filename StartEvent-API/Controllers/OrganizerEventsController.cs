using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StartEvent_API.Data;
using StartEvent_API.Data.Entities;
using System.Security.Claims;
using StartEvent_API.Models;
using StartEvent_API.Helper;

namespace StartEvent_API.Controllers
{
    [ApiController]
    [Route("api/organizer/events")]
    [Authorize(Roles = "Organizer")]
    public class OrganizerEventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorage _fileStorage;

        public OrganizerEventsController(ApplicationDbContext context, IFileStorage fileStorage)
        {
            _context = context;
            _fileStorage = fileStorage;
        }

        // GET: api/organizer/events
        [HttpGet]
        public async Task<IActionResult> GetMyEvents(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? category,
            [FromQuery] string? venue,
            [FromQuery] string? keyword,
            [FromQuery] bool? isPublished)
        {
            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(organizerId))
            {
                return Unauthorized("User not authenticated");
            }

            var query = _context.Events
                .Where(e => e.OrganizerId == organizerId)
                .Include(e => e.Venue)
                .AsQueryable();

            // Apply filters
            if (fromDate.HasValue)
                query = query.Where(e => e.EventDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(e => e.EventDate <= toDate.Value);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(e => e.Category.Contains(category));

            if (!string.IsNullOrEmpty(venue))
                query = query.Where(e => e.Venue.Name.Contains(venue));

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(e => e.Title.Contains(keyword) || e.Description.Contains(keyword));

            if (isPublished.HasValue)
                query = query.Where(e => e.IsPublished == isPublished.Value);

            var events = await query.OrderBy(e => e.EventDate).ToListAsync();

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
                VenueName = e.Venue != null ? e.Venue.Name ?? string.Empty : string.Empty
            }).ToList();

            return Ok(eventDtos);
        }

        // POST: api/organizer/events
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromForm] CreateEventDto createEventDto)
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

            // Handle image - either file upload or URL
            string? imagePath = null;
            if (createEventDto.ImageFile != null && createEventDto.ImageFile.Length > 0)
            {
                try
                {
                    // Validate file type
                    var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                    if (!allowedTypes.Contains(createEventDto.ImageFile.ContentType.ToLower()))
                    {
                        return BadRequest("Invalid file type. Only JPEG, PNG, GIF, and WebP images are allowed.");
                    }

                    // Validate file size (e.g., max 5MB)
                    if (createEventDto.ImageFile.Length > 5 * 1024 * 1024)
                    {
                        return BadRequest("File size cannot exceed 5MB.");
                    }

                    // Read file data
                    using var memoryStream = new MemoryStream();
                    await createEventDto.ImageFile.CopyToAsync(memoryStream);
                    var imageBytes = memoryStream.ToArray();

                    // Generate filename
                    var extension = Path.GetExtension(createEventDto.ImageFile.FileName);
                    var fileName = $"event_{Guid.NewGuid()}{extension}";

                    // Save image to storage
                    imagePath = await _fileStorage.SaveFileAsync(imageBytes, fileName, "events");
                }
                catch (Exception ex)
                {
                    return BadRequest($"Failed to process image: {ex.Message}");
                }
            }
            else if (!string.IsNullOrEmpty(createEventDto.ImageUrl))
            {
                // Use provided image URL
                imagePath = createEventDto.ImageUrl;
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
                Image = imagePath,
                VenueId = createEventDto.VenueId,
                IsPublished = createEventDto.IsPublished,
                OrganizerId = organizerId,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            // Add event prices if provided (parse from JSON string if provided)
            if (!string.IsNullOrEmpty(createEventDto.PricesJson))
            {
                try
                {
                    var prices = System.Text.Json.JsonSerializer.Deserialize<List<CreateEventPriceDto>>(createEventDto.PricesJson);
                    if (prices != null && prices.Any())
                    {
                        foreach (var priceDto in prices)
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
                }
                catch (Exception ex)
                {
                    return BadRequest($"Invalid prices JSON: {ex.Message}");
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
                EventTime = newEvent.EventTime,
                Category = newEvent.Category,
                Image = newEvent.Image,
                ImageUrl = !string.IsNullOrEmpty(newEvent.Image) ? await _fileStorage.GetFileUrlAsync(newEvent.Image) : null,
                IsPublished = newEvent.IsPublished,
                VenueId = newEvent.VenueId,
                VenueName = newEvent.Venue != null ? newEvent.Venue.Name ?? string.Empty : string.Empty
            };

            return CreatedAtAction(nameof(GetMyEvents), new { id = newEvent.Id }, eventDto);
        }

        // PUT: api/organizer/events/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(Guid id, [FromForm] UpdateEventDto updateEventDto)
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

            var existingEvent = await _context.Events
                .Include(e => e.Prices)
                .FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == organizerId);

            if (existingEvent == null)
            {
                return NotFound("Event not found or you don't have permission to edit it");
            }

            // Handle image - either file upload or URL
            if (updateEventDto.ImageFile != null && updateEventDto.ImageFile.Length > 0)
            {
                try
                {
                    // Validate file type
                    var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                    if (!allowedTypes.Contains(updateEventDto.ImageFile.ContentType.ToLower()))
                    {
                        return BadRequest("Invalid file type. Only JPEG, PNG, GIF, and WebP images are allowed.");
                    }

                    // Validate file size (e.g., max 5MB)
                    if (updateEventDto.ImageFile.Length > 5 * 1024 * 1024)
                    {
                        return BadRequest("File size cannot exceed 5MB.");
                    }

                    // Read file data
                    using var memoryStream = new MemoryStream();
                    await updateEventDto.ImageFile.CopyToAsync(memoryStream);
                    var imageBytes = memoryStream.ToArray();

                    // Generate filename
                    var extension = Path.GetExtension(updateEventDto.ImageFile.FileName);
                    var fileName = $"event_{Guid.NewGuid()}{extension}";

                    // Save image to storage
                    existingEvent.Image = await _fileStorage.SaveFileAsync(imageBytes, fileName, "events");
                }
                catch (Exception ex)
                {
                    return BadRequest($"Failed to process image: {ex.Message}");
                }
            }
            else if (!string.IsNullOrEmpty(updateEventDto.ImageUrl))
            {
                // Use provided image URL
                existingEvent.Image = updateEventDto.ImageUrl;
            }

            // Update event properties
            existingEvent.Title = updateEventDto.Title;
            existingEvent.Description = updateEventDto.Description;
            existingEvent.EventDate = updateEventDto.EventDate;
            existingEvent.EventTime = updateEventDto.EventTime;
            existingEvent.Category = updateEventDto.Category;
            existingEvent.VenueId = updateEventDto.VenueId;
            existingEvent.IsPublished = updateEventDto.IsPublished;
            existingEvent.ModifiedAt = DateTime.UtcNow;

            // Update event prices if provided (parse from JSON string)
            if (!string.IsNullOrEmpty(updateEventDto.PricesJson))
            {
                try
                {
                    var prices = System.Text.Json.JsonSerializer.Deserialize<List<UpdateEventPriceDto>>(updateEventDto.PricesJson);
                    if (prices != null)
                    {
                        // Remove existing prices
                        _context.EventPrices.RemoveRange(existingEvent.Prices);

                        // Add new prices
                        foreach (var priceDto in prices)
                        {
                            var eventPrice = new EventPrice
                            {
                                Id = Guid.NewGuid(),
                                EventId = existingEvent.Id,
                                Category = priceDto.Name,
                                Price = priceDto.Price,
                                Stock = priceDto.AvailableTickets,
                                IsActive = true
                            };
                            existingEvent.Prices.Add(eventPrice);
                        }
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest($"Invalid prices JSON: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            var eventDto = new EventDto
            {
                Id = existingEvent.Id,
                Title = existingEvent.Title,
                Description = existingEvent.Description,
                EventDate = existingEvent.EventDate,
                EventTime = existingEvent.EventTime,
                Category = existingEvent.Category,
                Image = existingEvent.Image,
                ImageUrl = !string.IsNullOrEmpty(existingEvent.Image) ? await _fileStorage.GetFileUrlAsync(existingEvent.Image) : null,
                IsPublished = existingEvent.IsPublished,
                VenueId = existingEvent.VenueId,
                VenueName = null
            };

            return Ok(eventDto);
        }
    }
}
